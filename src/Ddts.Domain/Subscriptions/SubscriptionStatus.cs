namespace Ddts.Domain.Subscriptions;

/// <summary>Abonelik durumu: Aktif / Süresi Doldu / İptal (FR-L4).</summary>
public enum SubscriptionStatus
{
    Active,
    Expired,
    Cancelled,
}
