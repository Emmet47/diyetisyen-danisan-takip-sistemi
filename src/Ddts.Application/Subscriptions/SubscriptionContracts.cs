using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Subscriptions;

/// <summary>Satıştaki paket (kayıt ekranı, web/API_REHBERI.md'deki SubscriptionPlan tipi).</summary>
public sealed record SubscriptionPlanDto(Guid Id, string Name, int MaxClients, decimal MonthlyPrice, bool IsActive)
{
    public static SubscriptionPlanDto From(SubscriptionPlan plan) =>
        new(plan.Id, plan.Name, plan.MaxClients, plan.MonthlyPrice, plan.IsActive);
}

/// <summary>Paket kullanımı, ör. 8/10. NearLimit: limitin %80'ine gelindi (panelde sarı uyarı, FR-L2).</summary>
public sealed record UsageDto(int ActiveClients, int MaxClients, bool NearLimit);

/// <summary>Diyetisyenin aboneliği: paket, durum, tarih ve kullanım (FR-L4).</summary>
public sealed record SubscriptionStatusDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    decimal MonthlyPrice,
    SubscriptionStatus Status,
    DateOnly StartDate,
    DateOnly EndDate,
    UsageDto Usage);
