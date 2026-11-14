using Ddts.Application.Abstractions.Auditing;
using Ddts.Domain.Auditing;
using Ddts.Infrastructure.Persistence;
using Ddts.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace Ddts.Infrastructure.Auditing;

/// <summary>
/// IAuditLogger'ın EF Core adapter'ı (Şekil 4.4). Kaydı yalnızca bağlama ekler; servisin
/// SaveChangesAsync çağrısıyla aynı işlemde yazılır (işlem başarısızsa denetim kaydı da yazılmaz).
/// </summary>
public sealed class EfAuditLogger(DdtsDbContext db, IHttpContextAccessor httpContextAccessor) : IAuditLogger
{
    public Task LogAsync(string action, string entityType, Guid? entityId, string? details, CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;

        db.AuditLogs.Add(new AuditLog(
            ActorUserId(httpContext, entityType, entityId),
            action,
            entityType,
            entityId,
            details,
            httpContext?.Connection.RemoteIpAddress?.ToString()));

        return Task.CompletedTask;
    }

    // İşlemi yapan: token'daki kullanıcı. Kayıt, giriş ve parola sıfırlamada henüz token yok;
    // orada işlemi yapan, kaydın kendisi olan kullanıcıdır (entityType = "User").
    private static Guid? ActorUserId(HttpContext? httpContext, string entityType, Guid? entityId)
    {
        var subject = httpContext?.User.FindFirst(DdtsClaims.Subject)?.Value;

        if (Guid.TryParse(subject, out var userId))
        {
            return userId;
        }

        return entityType == "User" ? entityId : null;
    }
}
