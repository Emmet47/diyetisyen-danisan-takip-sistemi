using Ddts.Domain.Common;

namespace Ddts.Domain.Auditing;

/// <summary>
/// Denetim kaydı (Şekil 4.1, FR-A5, NFR-G4): kim, ne zaman, hangi kayıtta ne yaptı.
/// Details alanı değişiklik geçmişini JSON olarak tutar: {"alan": {"old": .., "new": ..}} (FR-B3).
/// Kimlik veritabanında sırayla artan sayı (bigint identity).
/// </summary>
public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public AuditLog(Guid? userId, string action, string entityType, Guid? entityId, string? details, string? ipAddress)
    {
        UserId = userId;
        Action = Guard.Required(action, 50, "İşlem");
        EntityType = Guard.Required(entityType, 50, "Kayıt türü");
        EntityId = entityId;
        Details = details;
        IpAddress = Guard.Optional(ipAddress, 45, "IP adresi");
    }

    public long Id { get; private set; }

    /// <summary>İşlemi yapan kullanıcı; başarısız girişte boş olabilir.</summary>
    public Guid? UserId { get; private set; }

    /// <summary>LoginSucceeded, ClientUpdated, PlanPublished ...</summary>
    public string Action { get; private set; } = null!;

    /// <summary>Client, NutritionPlan, Appointment ...</summary>
    public string EntityType { get; private set; } = null!;

    public Guid? EntityId { get; private set; }

    /// <summary>JSON metni (veritabanında jsonb).</summary>
    public string? Details { get; private set; }

    public string? IpAddress { get; private set; }

    /// <summary>Kayıt anı; veritabanı yazar (DEFAULT now()).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Değer ayrıntılarını siler; "kim, ne zaman, hangi işlem" kalır. Danışan kalıcı silinince
    /// onun kayıtlarındaki sağlık verisi böyle temizlenir (belge 2.5, madde 4).
    /// </summary>
    public void ClearDetails() => Details = null;
}
