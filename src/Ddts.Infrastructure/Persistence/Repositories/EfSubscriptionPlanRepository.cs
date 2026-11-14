using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>ISubscriptionPlanRepository'nin EF Core uygulaması.</summary>
public sealed class EfSubscriptionPlanRepository(DdtsDbContext db) : ISubscriptionPlanRepository
{
    // Takip edilerek (tracking) okunur: yeni Subscription bu nesneyi gösterdiğinde EF onu
    // "zaten var" kabul eder ve paketi ikinci kez eklemeye çalışmaz.
    public Task<SubscriptionPlan?> GetAsync(Guid subscriptionPlanId, CancellationToken cancellationToken = default) =>
        db.SubscriptionPlans.SingleOrDefaultAsync(p => p.Id == subscriptionPlanId, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionPlan>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await db.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.MaxClients)
            .ToListAsync(cancellationToken);
}
