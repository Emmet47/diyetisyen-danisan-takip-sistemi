using Ddts.Domain.Common;

namespace Ddts.Domain.Notifications;

/// <summary>
/// Uygulama içi bildirim (Şekil 4.1, FR-J3). Metinde sağlık verisi olmaz (ör. "Yeni planın hazır").
/// DedupKey tekil: aynı hatırlatma (ör. "appt:{id}:24h") ikinci kez oluşturulamaz (BR-12).
/// </summary>
public sealed class Notification
{
    private Notification()
    {
    }

    public Notification(Guid userId, NotificationType type, string title, string body, string? dedupKey)
    {
        Id = Guid.CreateVersion7();
        UserId = userId;
        Type = type;
        Title = Guard.Required(title, 100, "Bildirim başlığı");
        Body = Guard.Required(body, 300, "Bildirim metni");
        DedupKey = Guard.Optional(dedupKey, 100, "Tekrar önleme anahtarı");
    }

    public Guid Id { get; private set; }

    /// <summary>Alıcı kullanıcı.</summary>
    public Guid UserId { get; private set; }

    public NotificationType Type { get; private set; }

    public string Title { get; private set; } = null!;

    public string Body { get; private set; } = null!;

    public bool IsRead { get; private set; }

    public string? DedupKey { get; private set; }

    /// <summary>Oluşturulma anı; veritabanı yazar (DEFAULT now()).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Telefona push gönderildiyse zamanı.</summary>
    public DateTime? SentAt { get; private set; }

    public void MarkRead() => IsRead = true;

    public void MarkSent(DateTime at) => SentAt = Guard.Utc(at, nameof(at));
}
