using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Measurements;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>
/// IMeasurementRepository'nin EF Core uygulaması. Sorgular (client_id, measured_on) tekil indeksini kullanır.
/// Danışanın diyetisyene ait olduğu servis tarafından önceden doğrulanır.
/// </summary>
public sealed class EfMeasurementRepository(DdtsDbContext db) : IMeasurementRepository
{
    public async Task<IReadOnlyList<Measurement>> ListAsync(
        Guid clientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await db.Measurements
            .AsNoTracking()
            .Where(m => m.ClientId == clientId && m.MeasuredOn >= from && m.MeasuredOn <= to)
            .OrderBy(m => m.MeasuredOn)
            .ToListAsync(cancellationToken);

    public Task<Measurement?> GetAsync(Guid clientId, Guid measurementId, CancellationToken cancellationToken = default) =>
        db.Measurements.SingleOrDefaultAsync(m => m.Id == measurementId && m.ClientId == clientId, cancellationToken);

    public void Add(Measurement measurement) => db.Measurements.Add(measurement);

    public void Remove(Measurement measurement) => db.Measurements.Remove(measurement);
}
