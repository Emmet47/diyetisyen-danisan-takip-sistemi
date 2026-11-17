using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.MealLogs;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>IMealLogRepository'nin EF Core uygulaması. Sorgular ix_meal_logs_client_date indeksini kullanır.</summary>
public sealed class EfMealLogRepository(DdtsDbContext db) : IMealLogRepository
{
    public async Task<IReadOnlyList<MealLog>> ListAsync(
        Guid clientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default) =>
        await db.MealLogs
            .AsNoTracking()
            .Where(l => l.ClientId == clientId && l.LogDate >= from && l.LogDate <= to)
            .OrderBy(l => l.LogDate)
            .ThenBy(l => l.LoggedAt)
            .ToListAsync(cancellationToken);

    public Task<MealLog?> GetAsync(Guid mealLogId, CancellationToken cancellationToken = default) =>
        db.MealLogs.SingleOrDefaultAsync(l => l.Id == mealLogId, cancellationToken);

    public async Task<IReadOnlyList<string>> ListPhotoKeysAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        // Fotoğraf isteğe bağlı bir complex type: süzme bellekte yapılır (danışan başına kayıt sayısı küçük).
        var logs = await db.MealLogs.AsNoTracking().Where(l => l.ClientId == clientId).ToListAsync(cancellationToken);
        return logs.Where(l => l.Photo is not null).Select(l => l.Photo!.Key).ToList();
    }

    public void Add(MealLog log) => db.MealLogs.Add(log);
}
