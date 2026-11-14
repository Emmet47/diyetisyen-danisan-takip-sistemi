using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Common;
using Ddts.Application.Subscriptions;

namespace Ddts.Application.Admin;

/// <summary>Gelen port: platform yöneticisinin işlemleri (UC-05…UC-07; FR-L1, FR-L5). Uygulaması AdminService.</summary>
public interface IAdminService
{
    /// <summary>Diyetisyenler, paketleri ve kullanımları; ada göre sıralı, sayfalı.</summary>
    Task<PagedResult<DietitianAdminDto>> ListDietitiansAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyen hesabını açar ya da kapatır; pasif hesap giriş yapamaz.</summary>
    Task SetDietitianActiveAsync(Guid dietitianId, bool isActive, CancellationToken cancellationToken = default);

    /// <summary>Satıştan kaldırılanlar dahil bütün paketler.</summary>
    Task<IReadOnlyList<SubscriptionPlanDto>> ListPackagesAsync(CancellationToken cancellationToken = default);

    /// <summary>subscriptionPlanId boşsa yeni paket oluşturur, doluysa o paketi düzenler.</summary>
    Task<SubscriptionPlanDto> SavePackageAsync(Guid? subscriptionPlanId, SavePackageCommand command, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyen başına aktif danışan / paket limiti; ada göre sıralı, sayfalı.</summary>
    Task<PagedResult<DietitianUsageDto>> GetUsageAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Denetim kayıtları ayrıntı alanı olmadan; en yenisi önce, sayfalı.</summary>
    Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);
}
