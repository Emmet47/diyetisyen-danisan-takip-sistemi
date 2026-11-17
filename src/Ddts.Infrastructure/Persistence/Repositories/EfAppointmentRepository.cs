using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Appointments;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>IAppointmentRepository'nin EF Core uygulaması.</summary>
public sealed class EfAppointmentRepository(DdtsDbContext db) : IAppointmentRepository
{
    public Task<Appointment?> GetAsync(Guid dietitianId, Guid appointmentId, CancellationToken cancellationToken = default) =>
        db.Appointments.SingleOrDefaultAsync(a => a.Id == appointmentId && a.DietitianId == dietitianId, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> ListActiveAsync(
        Guid dietitianId, DateOnly date, CancellationToken cancellationToken = default)
    {
        // Yerel gün UTC'de bir önceki günün akşamından başlar: aralığı bir gün geniş tutmak yeter,
        // slotlarla gerçekten kesişenleri AvailabilityService ayıklar.
        var from = date.AddDays(-1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to = date.AddDays(2).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        return await db.Appointments
            .AsNoTracking()
            .Where(a => a.DietitianId == dietitianId
                && (a.Status == AppointmentStatus.Requested || a.Status == AppointmentStatus.Confirmed)
                && a.StartsAt < to && a.EndsAt > from)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Appointment>> ListAsync(
        Guid dietitianId, DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        await db.Appointments
            .AsNoTracking()
            .Where(a => a.DietitianId == dietitianId && a.StartsAt >= from && a.StartsAt < to)
            .OrderBy(a => a.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> ListForClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        await db.Appointments
            .AsNoTracking()
            .Where(a => a.ClientId == clientId)
            .OrderByDescending(a => a.StartsAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> ListConfirmedStartingAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default) =>
        await db.Appointments
            .AsNoTracking()
            .Where(a => a.Status == AppointmentStatus.Confirmed && a.StartsAt > from && a.StartsAt <= to)
            .ToListAsync(cancellationToken);

    public void Add(Appointment appointment) => db.Appointments.Add(appointment);
}
