namespace Ddts.Application.Abstractions.Auditing;

/// <summary>
/// Giden port (Şekil 4.4): önemli işlemleri denetim kaydına yazar (FR-A5, NFR-G4).
/// Kayıt, servisin SaveChangesAsync çağrısıyla aynı veritabanı işleminde kaydedilir.
/// İşlemi yapan kullanıcı ve IP adresini adapter kendisi bulur.
/// </summary>
public interface IAuditLogger
{
    /// <param name="action">LoginSucceeded, DietitianRegistered, SubscriptionChanged ...</param>
    /// <param name="entityType">User, Client, Subscription ...</param>
    /// <param name="entityId">İlgili kaydın kimliği.</param>
    /// <param name="details">JSON metni; sağlık verisi yazılmaz.</param>
    Task LogAsync(string action, string entityType, Guid? entityId, string? details, CancellationToken cancellationToken = default);
}
