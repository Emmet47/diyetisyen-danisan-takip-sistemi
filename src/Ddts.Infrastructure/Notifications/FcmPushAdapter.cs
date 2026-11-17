using Ddts.Application.Abstractions.Notifications;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ddts.Infrastructure.Notifications;

/// <summary>
/// IPushNotificationSender'ın Firebase Cloud Messaging adapter'ı (Şekil 4.4, 4.11). Kimlik bilgisi dosyası
/// ayarlı değilse push atlanır (geliştirme ve testlerde). Gönderim hatası loglanır ve yukarı iletilir;
/// NotificationService bildirimi yine uygulama içinde tutar.
/// </summary>
public sealed class FcmPushAdapter(IOptions<FcmOptions> options, ILogger<FcmPushAdapter> logger) : IPushNotificationSender
{
    private const string AppName = "ddts";
    private static readonly Lock CreateLock = new();

    public async Task SendAsync(string pushToken, string title, string body, CancellationToken cancellationToken = default)
    {
        var app = GetApp();
        if (app is null)
        {
            logger.LogDebug("FCM ayarlı değil (Fcm:CredentialsPath); push atlandı.");
            return;
        }

        var message = new Message
        {
            Token = pushToken,
            Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
        };

        try
        {
            await FirebaseMessaging.GetMessaging(app).SendAsync(message, cancellationToken);
        }
        catch (FirebaseMessagingException exception)
        {
            // Token ve metin loga yazılmaz (kişisel veri); yalnızca hata kodu.
            logger.LogWarning("FCM push gönderilemedi: {Code}", exception.MessagingErrorCode);
            throw;
        }
    }

    private FirebaseApp? GetApp()
    {
        var path = options.Value.CredentialsPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        lock (CreateLock)
        {
#pragma warning disable CS0612, CS0618 // GoogleCredential.FromFile yeni sürümlerde "eski" işaretli; hizmet hesabı dosyası için yeterli.
            return FirebaseApp.GetInstance(AppName)
                ?? FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(path) }, AppName);
#pragma warning restore CS0612, CS0618
        }
    }
}
