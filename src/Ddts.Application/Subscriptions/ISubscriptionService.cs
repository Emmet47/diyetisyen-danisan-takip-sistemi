namespace Ddts.Application.Subscriptions;

/// <summary>Gelen port: paketler ve diyetisyenin aboneliği (L modülü, UC-04).</summary>
public interface ISubscriptionService
{
    /// <summary>Satıştaki paketler (herkese açık, kayıt ekranı için).</summary>
    Task<IReadOnlyList<SubscriptionPlanDto>> ListPlansAsync(CancellationToken cancellationToken = default);

    /// <summary>Giriş yapan diyetisyenin aboneliği ve kullanımı.</summary>
    Task<SubscriptionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Paket yükseltme/düşürme (FR-L3). Düşürmede aktif danışan sayısı yeni limiti aşamaz (BR-02).</summary>
    Task<SubscriptionStatusDto> ChangePlanAsync(Guid subscriptionPlanId, CancellationToken cancellationToken = default);

    /// <summary>Ödeme simülasyonuyla bir ay uzatma (FR-L4).</summary>
    Task<SubscriptionStatusDto> RenewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bitiş tarihi geçen aktif abonelikleri "süresi doldu" yapar (FR-L4); kaç abonelik değiştiğini döner.
    /// ReminderWorker 10 dakikada bir çağırır.
    /// </summary>
    Task<int> ExpireDueAsync(CancellationToken cancellationToken = default);
}
