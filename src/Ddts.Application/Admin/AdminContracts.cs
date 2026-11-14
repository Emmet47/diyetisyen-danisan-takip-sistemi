using Ddts.Application.Subscriptions;
using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Admin;

/// <summary>Yönetici listesindeki diyetisyen: hesap, paket ve kullanım (FR-L5). Sağlık verisi içermez.</summary>
public sealed record DietitianAdminDto(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string? Phone,
    bool IsActive,
    string? PlanName,
    SubscriptionStatus? SubscriptionStatus,
    DateOnly? SubscriptionEndDate,
    UsageDto Usage);

/// <summary>Diyetisyen başına kullanım, yalnızca sayı olarak (ör. 8/10).</summary>
public sealed record DietitianUsageDto(Guid DietitianId, string FullName, string? PlanName, UsageDto Usage);

/// <summary>Paket oluşturma/düzenleme formu (FR-L1). IsActive = false: satıştan kaldırılır.</summary>
public sealed record SavePackageCommand(string Name, int MaxClients, decimal MonthlyPrice, bool IsActive);

/// <summary>
/// Yöneticinin gördüğü denetim kaydı. Details (eski/yeni değerler) bilerek yok: yönetici sağlık
/// verisine erişemez (belge 2.5, madde 5).
/// </summary>
public sealed record AuditLogDto(long Id, DateTime At, Guid? UserId, string Action, string EntityType, Guid? EntityId, string? IpAddress);
