namespace Ddts.Infrastructure.Notifications;

/// <summary>
/// Firebase Cloud Messaging ayarları ("Fcm" bölümü). CredentialsPath: Firebase hizmet hesabı JSON dosyasının
/// sunucudaki yolu (dosya repoya girmez, NFR-G5). Boşsa push gönderilmez; bildirimler uygulama içinde görünür.
/// </summary>
public sealed class FcmOptions
{
    public const string SectionName = "Fcm";

    public string? CredentialsPath { get; set; }
}
