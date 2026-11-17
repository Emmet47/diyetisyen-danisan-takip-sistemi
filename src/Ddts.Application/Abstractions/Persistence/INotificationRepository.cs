using Ddts.Domain.Notifications;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Uygulama içi bildirimler (Şekil 4.10, J modülü). Kullanıcı yalnızca kendi bildirimlerini görür.</summary>
public interface INotificationRepository
{
    /// <summary>Kullanıcının bildirimleri, en yenisi önce, sayfalı.</summary>
    Task<IReadOnlyList<Notification>> ListForUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Notification?> GetAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>Bu tekrar önleme anahtarıyla bildirim üretilmiş mi? (BR-12, aynı hatırlatma iki kez gitmez)</summary>
    Task<bool> DedupKeyExistsAsync(string dedupKey, CancellationToken cancellationToken = default);

    void Add(Notification notification);
}
