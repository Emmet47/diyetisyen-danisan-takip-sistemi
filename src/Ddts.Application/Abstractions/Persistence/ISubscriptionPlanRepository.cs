using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Abonelik paketleri. Repository deseni, Şekil 4.10.</summary>
public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetAsync(Guid subscriptionPlanId, CancellationToken cancellationToken = default);

    /// <summary>Satıştaki paketler, danışan limitine göre sıralı.</summary>
    Task<IReadOnlyList<SubscriptionPlan>> ListActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Satıştan kaldırılanlar dahil bütün paketler (yönetici, FR-L1).</summary>
    Task<IReadOnlyList<SubscriptionPlan>> ListAllAsync(CancellationToken cancellationToken = default);

    void Add(SubscriptionPlan plan);
}
