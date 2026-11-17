using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Appointments;
using Ddts.Domain.Common;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Users;

namespace Ddts.Application.Appointments;

/// <summary>
/// Randevu use case'leri (I modülü, Şekil 5.7). Slot kuralları (BR-08): çalışma saati içinde, 30 dakikalık slotun
/// başında, gelecekte ve boş olmalı. Aynı anda gelen iki talepten birini veritabanındaki EXCLUDE kısıtı durdurur
/// (FR-I4) → 409. Durum değişiklikleri domain event üretir; bildirimler commit'ten sonra gider (Şekil 4.11).
/// </summary>
public sealed class AppointmentService(
    IAppointmentRepository appointments,
    IDietitianRepository dietitians,
    IClientRepository clients,
    AvailabilityService availability,
    IUnitOfWork unitOfWork,
    IAuditLogger auditLogger,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IAppointmentService
{
    public const string AuditEntityType = "Appointment";
    public const string SlotTakenMessage = "Lütfen başka bir saat seç.";

    public async Task SetWorkingHoursAsync(IReadOnlyList<WorkingHourDto> hours, CancellationToken cancellationToken = default)
    {
        var dietitian = await GetDietitianAsync(DietitianId(), cancellationToken);
        dietitian.SetWorkingHours((hours ?? []).Select(h => new WorkingHour(h.Weekday, h.StartTime, h.EndTime)));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkingHourDto>> GetWorkingHoursAsync(CancellationToken cancellationToken = default)
    {
        var dietitian = await GetDietitianAsync(DietitianId(), cancellationToken);
        return dietitian.WorkingHours
            .OrderBy(h => h.Weekday)
            .Select(h => new WorkingHourDto(h.Weekday, h.StartTime, h.EndTime))
            .ToList();
    }

    public async Task<IReadOnlyList<SlotDto>> GetFreeSlotsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dietitian = await GetDietitianAsync(MyDietitianId(), cancellationToken);
        var slots = await FreeFutureSlotsAsync(dietitian, date, exceptAppointmentId: null, cancellationToken);
        return slots.Select(s => new SlotDto(s.Start, s.End)).ToList();
    }

    public async Task<AppointmentDto> RequestAsync(DateTime startsAt, CancellationToken cancellationToken = default)
    {
        var dietitian = await GetDietitianAsync(MyDietitianId(), cancellationToken);
        var client = await clients.GetAsync(dietitian.Id, ClientId(), cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        var slot = await RequireFreeSlotAsync(dietitian, startsAt, exceptAppointmentId: null, cancellationToken);
        var appointment = Appointment.Request(dietitian, client, slot);

        appointments.Add(appointment);
        await auditLogger.LogAsync("AppointmentRequested", AuditEntityType, appointment.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(appointment, clientName: null);
    }

    public async Task<AppointmentDto> CreateConfirmedAsync(Guid clientId, DateTime startsAt, CancellationToken cancellationToken = default)
    {
        var dietitian = await GetDietitianAsync(DietitianId(), cancellationToken);
        var client = await clients.GetAsync(dietitian.Id, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        var slot = await RequireFreeSlotAsync(dietitian, startsAt, exceptAppointmentId: null, cancellationToken);
        var appointment = Appointment.CreateConfirmed(dietitian, client, slot);

        appointments.Add(appointment);
        await auditLogger.LogAsync("AppointmentCreated", AuditEntityType, appointment.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(appointment, client.FullName);
    }

    public async Task<AppointmentDto> ConfirmAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetForDietitianAsync(appointmentId, cancellationToken);
        appointment.Confirm();
        return await SaveAsync(appointment, "AppointmentConfirmed", cancellationToken);
    }

    public async Task<AppointmentDto> CancelAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetForParticipantAsync(appointmentId, cancellationToken);
        appointment.Cancel(currentUser.Role);
        return await SaveAsync(appointment, "AppointmentCancelled", cancellationToken);
    }

    public async Task<AppointmentDto> RescheduleAsync(Guid appointmentId, DateTime startsAt, CancellationToken cancellationToken = default)
    {
        var appointment = await GetForParticipantAsync(appointmentId, cancellationToken);
        var dietitian = await GetDietitianAsync(appointment.DietitianId, cancellationToken);

        // Randevunun kendi slotu "dolu" sayılmaz: aynı gün içinde kaydırılabilir.
        var slot = await RequireFreeSlotAsync(dietitian, startsAt, appointment.Id, cancellationToken);
        appointment.Reschedule(slot, currentUser.Role);
        return await SaveAsync(appointment, "AppointmentRescheduled", cancellationToken);
    }

    public async Task<AppointmentDto> CompleteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var appointment = await GetForDietitianAsync(appointmentId, cancellationToken);
        appointment.Complete(timeProvider.GetUtcNow().UtcDateTime);
        return await SaveAsync(appointment, "AppointmentCompleted", cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDto>> ListAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var start = from is { } f ? LocalTime.RequireUtc(f, "from") : LocalTime.StartOfDayUtc(LocalTime.Today(timeProvider));
        var end = to is { } t ? LocalTime.RequireUtc(t, "to") : start.AddDays(30);

        if (start >= end)
        {
            throw new DomainException("Başlangıç zamanı bitişten önce olmalı.");
        }

        var list = await appointments.ListAsync(dietitianId, start, end, cancellationToken);
        var names = (await clients.ListAsync(dietitianId, onlyActive: false, cancellationToken))
            .ToDictionary(c => c.Id, c => c.FullName);

        return list.Select(a => ToDto(a, names.GetValueOrDefault(a.ClientId))).ToList();
    }

    public async Task<IReadOnlyList<AppointmentDto>> ListMyAsync(CancellationToken cancellationToken = default)
    {
        var list = await appointments.ListForClientAsync(ClientId(), cancellationToken);
        return list.Select(a => ToDto(a, clientName: null)).ToList();
    }

    /// <summary>İstenen başlangıç geçerli ve boş bir slot mu? Değilse 400 (uygun değil) ya da 409 (dolu).</summary>
    private async Task<TimeSlot> RequireFreeSlotAsync(
        Dietitian dietitian, DateTime startsAt, Guid? exceptAppointmentId, CancellationToken cancellationToken)
    {
        var start = LocalTime.RequireUtc(startsAt, "startsAt");
        var date = LocalTime.DateOf(start);

        var free = await FreeFutureSlotsAsync(dietitian, date, exceptAppointmentId, cancellationToken);
        var slot = free.FirstOrDefault(s => s.Start == start);
        if (slot is not null)
        {
            return slot;
        }

        // Boş değil ama geçerli bir slotsa başka bir randevu almış demektir (Şekil 5.7: "Bu saat az önce doldu").
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var isWorkingSlot = availability.GetFreeSlots(dietitian.WorkingHours, date, [])
            .Any(s => s.Start == start && s.Start > now);

        throw isWorkingSlot
            ? new SlotTakenException(SlotTakenMessage)
            : new DomainException("Seçilen saat uygun değil: çalışma saatleri içinde, 30 dakikalık bir slotun başında ve ileri bir zamanda olmalı.");
    }

    private async Task<IReadOnlyList<TimeSlot>> FreeFutureSlotsAsync(
        Dietitian dietitian, DateOnly date, Guid? exceptAppointmentId, CancellationToken cancellationToken)
    {
        var booked = (await appointments.ListActiveAsync(dietitian.Id, date, cancellationToken))
            .Where(a => a.Id != exceptAppointmentId);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return availability.GetFreeSlots(dietitian.WorkingHours, date, booked)
            .Where(s => s.Start > now)
            .ToList();
    }

    private async Task<AppointmentDto> SaveAsync(Appointment appointment, string action, CancellationToken cancellationToken)
    {
        await auditLogger.LogAsync(action, AuditEntityType, appointment.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(appointment, clientName: null);
    }

    private async Task<Appointment> GetForDietitianAsync(Guid appointmentId, CancellationToken cancellationToken) =>
        await appointments.GetAsync(DietitianId(), appointmentId, cancellationToken)
        ?? throw new NotFoundException("Randevu bulunamadı.");

    /// <summary>Diyetisyen kendi randevusunu, danışan kendi randevusunu getirir; başkasınınki 404.</summary>
    private async Task<Appointment> GetForParticipantAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        if (currentUser.Role == UserRole.Dietitian)
        {
            return await GetForDietitianAsync(appointmentId, cancellationToken);
        }

        var appointment = await appointments.GetAsync(MyDietitianId(), appointmentId, cancellationToken);
        if (appointment is null || appointment.ClientId != ClientId())
        {
            throw new NotFoundException("Randevu bulunamadı.");
        }

        return appointment;
    }

    private async Task<Dietitian> GetDietitianAsync(Guid dietitianId, CancellationToken cancellationToken) =>
        await dietitians.GetAsync(dietitianId, cancellationToken)
        ?? throw new NotFoundException("Diyetisyen bulunamadı.");

    private static AppointmentDto ToDto(Appointment a, string? clientName) =>
        new(a.Id, a.ClientId, clientName, a.StartsAt, a.EndsAt, a.Status, a.Note);

    private Guid DietitianId() =>
        currentUser.Role == UserRole.Dietitian && currentUser.DietitianId is { } id
            ? id
            : throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

    /// <summary>Danışanın bağlı olduğu diyetisyen (token'daki dietitian_id).</summary>
    private Guid MyDietitianId() =>
        currentUser.DietitianId ?? throw new InvalidOperationException("Danışanın bağlı olduğu diyetisyen token'da yok.");

    private Guid ClientId() =>
        currentUser.ClientId ?? throw new InvalidOperationException("Bu işlem yalnızca danışan hesabıyla yapılabilir.");
}
