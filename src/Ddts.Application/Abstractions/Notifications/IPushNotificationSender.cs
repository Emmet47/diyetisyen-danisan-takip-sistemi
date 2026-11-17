namespace Ddts.Application.Abstractions.Notifications;

/// <summary>
/// Telefona anlık bildirim (push) portu (Şekil 4.4). Adapter: FcmPushAdapter (Firebase Cloud Messaging).
/// Metinlerde sağlık verisi bulunmaz (KVKK, belge 2.5 madde 3). Gönderilemezse hata fırlatır.
/// </summary>
public interface IPushNotificationSender
{
    Task SendAsync(string pushToken, string title, string body, CancellationToken cancellationToken = default);
}
