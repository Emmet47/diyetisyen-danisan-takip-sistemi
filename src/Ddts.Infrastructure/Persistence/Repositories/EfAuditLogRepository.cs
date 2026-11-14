using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>IAuditLogRepository'nin EF Core uygulaması (ix_audit_logs_entity indeksini kullanır).</summary>
public sealed class EfAuditLogRepository(DdtsDbContext db) : IAuditLogRepository
{
    // Kayıtlar takip edilir (tracking): danışan silinirken ayrıntıları temizlenip kaydedilebilsin.
    public async Task<IReadOnlyList<AuditLog>> ListForEntityAsync(
        string entityType, Guid entityId, CancellationToken cancellationToken = default) =>
        await db.AuditLogs
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync(cancellationToken);
}
