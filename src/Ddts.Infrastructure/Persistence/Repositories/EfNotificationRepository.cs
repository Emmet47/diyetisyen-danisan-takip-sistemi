using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>INotificationRepository'nin EF Core uygulaması. Liste ix_notifications_user indeksini kullanır.</summary>
public sealed class EfNotificationRepository(DdtsDbContext db) : INotificationRepository
{
    public async Task<IReadOnlyList<Notification>> ListForUserAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Notifications.CountAsync(n => n.UserId == userId, cancellationToken);

    public Task<Notification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default) =>
        db.Notifications.SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

    public Task<bool> DedupKeyExistsAsync(string dedupKey, CancellationToken cancellationToken = default) =>
        db.Notifications.AnyAsync(n => n.DedupKey == dedupKey, cancellationToken);

    public void Add(Notification notification) => db.Notifications.Add(notification);
}
