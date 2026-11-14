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

    /// <summary>Süzgece uyan kayıtlardan bir sayfa, en yenisi önce (yönetici, FR-L5).</summary>
    Task<IReadOnlyList<AuditLog>> ListAsync(AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(AuditLogFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// Denetim kaydı süzgeci. From/To gün olarak verilir ve ikisi de dahildir (UTC günleri);
/// boş alan süzülmez.
/// </summary>
public sealed record AuditLogFilter(DateOnly? From, DateOnly? To, Guid? UserId, string? Action);
