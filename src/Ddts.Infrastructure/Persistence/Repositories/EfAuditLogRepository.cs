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

    public async Task<IReadOnlyList<AuditLog>> ListAsync(
        AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default) =>
        await Filtered(filter)
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(AuditLogFilter filter, CancellationToken cancellationToken = default) =>
        Filtered(filter).CountAsync(cancellationToken);

    private IQueryable<AuditLog> Filtered(AuditLogFilter filter)
    {
        var query = db.AuditLogs.AsNoTracking();

        // Günler UTC; "to" günü de dahil: to + 1 günün başından öncesi.
        if (filter.From is { } from)
        {
            var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAt >= fromUtc);
        }

        if (filter.To is { } to)
        {
            var toUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAt < toUtc);
        }

        if (filter.UserId is { } userId)
        {
            query = query.Where(a => a.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim();
            query = query.Where(a => a.Action == action);
        }

        return query;
    }
}
