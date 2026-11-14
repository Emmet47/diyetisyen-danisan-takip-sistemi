using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>ISubscriptionRepository'nin EF Core uygulaması (Şekil 4.10'daki EfSubscriptionRepository).</summary>
public sealed class EfSubscriptionRepository(DdtsDbContext db) : ISubscriptionRepository
{
    public async Task<Subscription?> GetActiveForUpdateAsync(Guid dietitianId, CancellationToken cancellationToken = default)
    {
        // FOR UPDATE: satır, açık veritabanı işlemi (transaction) bitene kadar kilitli kalır.
        // {dietitianId} SQL'e parametre olarak gider (SQL injection olmaz).
        var subscription = await db.Subscriptions
            .FromSql($"SELECT * FROM subscriptions WHERE dietitian_id = {dietitianId} AND status = 'Active' FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (subscription is not null)
        {
            await db.Entry(subscription).Reference(s => s.Plan).LoadAsync(cancellationToken);
        }

        return subscription;
    }

    public async Task<Subscription?> GetCurrentAsync(Guid dietitianId, CancellationToken cancellationToken = default)
    {
        var dietitianSubscriptions = db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.DietitianId == dietitianId);

        return await dietitianSubscriptions.FirstOrDefaultAsync(s => s.Status == SubscriptionStatus.Active, cancellationToken)
            ?? await dietitianSubscriptions.OrderByDescending(s => s.EndDate).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> ListDueForExpiryAsync(DateOnly today, CancellationToken cancellationToken = default) =>
        await db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < today)
            .ToListAsync(cancellationToken);

    public void Add(Subscription subscription) => db.Subscriptions.Add(subscription);
}
