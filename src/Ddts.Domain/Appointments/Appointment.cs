using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Users;

namespace Ddts.Domain.Appointments;

/// <summary>
/// Randevu (Şekil 4.1). Durum geçişleri BR-09'a göre; her geçiş bir domain event üretir (Şekil 4.11).
/// Çakışma kontrolünün son sözü veritabanında: EXCLUDE kısıtı aynı diyetisyenin iki aktif
/// randevusunun kesişmesini, aynı anda gelen iki istekte bile engeller (ADR-0002).
/// </summary>
public sealed class Appointment : AggregateRoot
{
    private Appointment()
    {
    }

    private Appointment(Dietitian dietitian, Client client, TimeSlot slot, AppointmentStatus status, string? note)
    {
        if (client.DietitianId != dietitian.Id)
        {
            throw new DomainException("Danışan bu diyetisyene bağlı değil.");
        }

        Id = Guid.CreateVersion7();
        DietitianId = dietitian.Id;
        ClientId = client.Id;
        StartsAt = slot.Start;
        EndsAt = slot.End;
        Status = status;
        Note = Guard.Optional(note, 300, "Not");
    }

    public Guid Id { get; private set; }

    public Guid DietitianId { get; private set; }

    public Guid ClientId { get; private set; }

    public DateTime StartsAt { get; private set; }

    public DateTime EndsAt { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public string? Note { get; private set; }

    /// <summary>Danışanın talebi: Talep Edildi durumunda başlar (FR-I2).</summary>
    public static Appointment Request(Dietitian dietitian, Client client, TimeSlot slot)
    {
        var appointment = new Appointment(dietitian, client, slot, AppointmentStatus.Requested, note: null);
        appointment.Raise(new AppointmentRequested(appointment.Id, appointment.DietitianId, appointment.ClientId, DateTime.UtcNow));
        return appointment;
    }

    /// <summary>Diyetisyenin doğrudan oluşturduğu randevu: onaylı başlar (FR-I2).</summary>
    public static Appointment CreateConfirmed(Dietitian dietitian, Client client, TimeSlot slot)
    {
        var appointment = new Appointment(dietitian, client, slot, AppointmentStatus.Confirmed, note: null);
        appointment.Raise(new AppointmentConfirmed(appointment.Id, appointment.DietitianId, appointment.ClientId, DateTime.UtcNow));
        return appointment;
    }

    /// <summary>Talep Edildi → Onaylandı (yalnızca diyetisyen; rol kontrolü API'de).</summary>
    public void Confirm()
    {
        if (Status != AppointmentStatus.Requested)
        {
            throw new DomainException("Yalnızca talep edilmiş randevu onaylanabilir.");
        }

        Status = AppointmentStatus.Confirmed;
        Raise(new AppointmentConfirmed(Id, DietitianId, ClientId, DateTime.UtcNow));
    }

    /// <summary>Talep Edildi / Onaylandı → İptal (iki taraf da yapabilir).</summary>
    public void Cancel(UserRole by)
    {
        EnsureParticipant(by);
        EnsureActive();

        Status = AppointmentStatus.Cancelled;
        Raise(new AppointmentCancelled(Id, DietitianId, ClientId, DateTime.UtcNow, by));
    }

    /// <summary>
    /// Erteleme: danışan ertelerse randevu yeniden Talep Edildi olur, diyetisyen ertelerse
    /// Onaylandı olur (BR-09). Yeni saatin boş olduğunu EXCLUDE kısıtı da denetler.
    /// </summary>
    public void Reschedule(TimeSlot slot, UserRole by)
    {
        EnsureParticipant(by);
        EnsureActive();

        StartsAt = slot.Start;
        EndsAt = slot.End;
        Status = by == UserRole.Client ? AppointmentStatus.Requested : AppointmentStatus.Confirmed;
        Raise(new AppointmentRescheduled(Id, DietitianId, ClientId, DateTime.UtcNow, by));
    }

    /// <summary>Onaylı ve saati gelmiş randevu tamamlandı olarak işaretlenir (FR-I3).</summary>
    public void Complete(DateTime now)
    {
        if (Status != AppointmentStatus.Confirmed)
        {
            throw new DomainException("Yalnızca onaylı randevu tamamlanabilir.");
        }

        if (now < StartsAt)
        {
            throw new DomainException("Saati gelmemiş randevu tamamlanamaz.");
        }

        Status = AppointmentStatus.Completed;
    }

    private void EnsureActive()
    {
        if (Status is not (AppointmentStatus.Requested or AppointmentStatus.Confirmed))
        {
            throw new DomainException("İptal edilmiş ya da tamamlanmış randevu değiştirilemez.");
        }
    }

    private static void EnsureParticipant(UserRole by)
    {
        if (by is not (UserRole.Dietitian or UserRole.Client))
        {
            throw new DomainException("Randevuyu yalnızca diyetisyen ya da danışan değiştirebilir.");
        }
    }
}
