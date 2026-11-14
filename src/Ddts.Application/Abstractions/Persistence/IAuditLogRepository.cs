using Ddts.Domain.Auditing;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Denetim kayıtlarını okur (Şekil 4.10). Kayıt yazmak IAuditLogger'ın işi; bu arayüz değişiklik
/// geçmişini (FR-B3) listeler.
/// </summary>
public interface IAuditLogRepository
{
    /// <summary>Bir kaydın (ör. Client + danışan kimliği) bütün denetim kayıtları, en yenisi önce.</summary>
    Task<IReadOnlyList<AuditLog>> ListForEntityAsync(string entityType, Guid entityId, CancellationToken cancellationToken = default);
}
