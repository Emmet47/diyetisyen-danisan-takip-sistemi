using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Diyetisyen abonelikleri (Şekil 4.10). Dönen abonelik paketiyle (Plan) birlikte yüklenir.</summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Aktif aboneliği satırı kilitleyerek okur (SELECT … FOR UPDATE). Bir veritabanı işlemi
    /// (ExecuteInTransactionAsync) içinde çağrılır; aynı anda gelen iki istek limiti aşamaz (Şekil 5.1).
    /// </summary>
    Task<Subscription?> GetActiveForUpdateAsync(Guid dietitianId, CancellationToken cancellationToken = default);

    /// <summary>Aktif abonelik; yoksa bitiş tarihi en geç olan (süresi dolmuş ya da iptal edilmiş) abonelik.</summary>
    Task<Subscription?> GetCurrentAsync(Guid dietitianId, CancellationToken cancellationToken = default);

    /// <summary>Bitiş tarihi geçmiş (bugünden önce) ama hâlâ Active görünen abonelikler, paketleriyle.</summary>
    Task<IReadOnlyList<Subscription>> ListDueForExpiryAsync(DateOnly today, CancellationToken cancellationToken = default);

    void Add(Subscription subscription);
}
