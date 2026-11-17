using Ddts.Application.Common;
using Ddts.Domain.Notifications;

namespace Ddts.Application.Notifications;

/// <summary>Gelen port: bildirimler (J modülü, FR-J1–J3, Şekil 4.11).</summary>
public interface INotificationService
{
    /// <summary>
    /// Bildirim üretir: danışanın kapattığı türdeyse ya da aynı dedupKey ile daha önce üretildiyse hiçbir şey
    /// yapmaz (BR-12). Uygulama içi listeye yazar; telefonun push anahtarı varsa push da gönderir.
    /// </summary>
    Task NotifyAsync(Guid userId, NotificationType type, string title, string body, string? dedupKey, CancellationToken cancellationToken = default);

    /// <summary>Giriş yapmış kullanıcının bildirimleri, en yenisi önce.</summary>
    Task<PagedResult<NotificationDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default);

    /// <summary>Danışan bildirim izinlerini değiştirir.</summary>
    Task UpdateSettingsAsync(NotificationSettingsDto settings, CancellationToken cancellationToken = default);

    /// <summary>Danışanın bildirim izinleri.</summary>
    Task<NotificationSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>Telefonun FCM anahtarını kaydeder (boş gönderilirse siler: push kapanır).</summary>
    Task UpdatePushTokenAsync(string? pushToken, CancellationToken cancellationToken = default);
}
