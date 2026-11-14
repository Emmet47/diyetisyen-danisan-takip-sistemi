using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Common;
using Ddts.Application.Subscriptions;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Admin;

/// <summary>
/// Platform yöneticisinin işlemleri (FR-L1, FR-L5). Yönetici danışan verisine erişmez: yalnızca
/// hesapları, paketleri, kullanım sayılarını ve ayrıntısız denetim kayıtlarını görür (belge 2.5, madde 5).
/// </summary>
public sealed class AdminService(
    IDietitianRepository dietitians,
    IUserRepository users,
    ISubscriptionRepository subscriptions,
    ISubscriptionPlanRepository subscriptionPlans,
    IClientRepository clients,
    IAuditLogRepository auditLogs,
    IUnitOfWork unitOfWork,
    IAuditLogger auditLogger) : IAdminService
{
    public async Task<PagedResult<DietitianAdminDto>> ListDietitiansAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        Paging.Ensure(page, pageSize);

        var total = await dietitians.CountAsync(cancellationToken);
        var pageItems = await dietitians.ListAsync(page, pageSize, cancellationToken);
        var accounts = (await users.GetManyAsync(pageItems.Select(d => d.UserId).ToList(), cancellationToken))
            .ToDictionary(u => u.Id);

        var items = new List<DietitianAdminDto>();
        foreach (var dietitian in pageItems)
        {
            var account = accounts[dietitian.UserId];
            var (subscription, usage) = await UsageOfAsync(dietitian, cancellationToken);

            items.Add(new DietitianAdminDto(
                dietitian.Id,
                dietitian.UserId,
                dietitian.FullName,
                account.Email,
                dietitian.Phone,
                account.IsActive,
                subscription?.Plan.Name,
                subscription?.Status,
                subscription?.EndDate,
                usage));
        }

        return new PagedResult<DietitianAdminDto>(items, page, pageSize, total);
    }

    public async Task SetDietitianActiveAsync(Guid dietitianId, bool isActive, CancellationToken cancellationToken = default)
    {
        var dietitian = await dietitians.GetAsync(dietitianId, cancellationToken)
            ?? throw new NotFoundException("Diyetisyen bulunamadı.");
        var account = await users.GetAsync(dietitian.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Diyetisyenin giriş hesabı bulunamadı.");

        if (account.IsActive == isActive)
        {
            throw new ConflictException(isActive ? "Hesap zaten aktif." : "Hesap zaten pasif.");
        }

        // Pasif hesap giriş yapamaz; elindeki token da bir sonraki istekte geçersiz olur (ADR-0006).
        if (isActive)
        {
            account.Activate();
        }
        else
        {
            account.Deactivate();
        }

        await auditLogger.LogAsync(
            isActive ? "DietitianActivated" : "DietitianDeactivated", "Dietitian", dietitian.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> ListPackagesAsync(CancellationToken cancellationToken = default)
    {
        var plans = await subscriptionPlans.ListAllAsync(cancellationToken);
        return plans.Select(SubscriptionPlanDto.From).ToList();
    }

    public async Task<SubscriptionPlanDto> SavePackageAsync(
        Guid? subscriptionPlanId, SavePackageCommand command, CancellationToken cancellationToken = default)
    {
        // Paket adı tekil (subscription_plans.name UNIQUE); kullanıcıya anlaşılır bir hata verelim.
        var allPlans = await subscriptionPlans.ListAllAsync(cancellationToken);
        var name = command.Name.Trim();
        if (allPlans.Any(p => p.Id != subscriptionPlanId && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException("Bu adla bir paket zaten var.");
        }

        SubscriptionPlan plan;
        if (subscriptionPlanId is null)
        {
            plan = new SubscriptionPlan(command.Name, command.MaxClients, command.MonthlyPrice);
            if (!command.IsActive)
            {
                plan.Update(command.Name, command.MaxClients, command.MonthlyPrice, isActive: false);
            }

            subscriptionPlans.Add(plan);
            await auditLogger.LogAsync("SubscriptionPlanCreated", "SubscriptionPlan", plan.Id, null, cancellationToken);
        }
        else
        {
            plan = await subscriptionPlans.GetAsync(subscriptionPlanId.Value, cancellationToken)
                ?? throw new NotFoundException("Paket bulunamadı.");

            // Paketi kullanan diyetisyenlerin aboneliği sürer; satıştan kaldırılan paket yeni kayıtta seçilemez.
            plan.Update(command.Name, command.MaxClients, command.MonthlyPrice, command.IsActive);
            await auditLogger.LogAsync("SubscriptionPlanUpdated", "SubscriptionPlan", plan.Id, null, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SubscriptionPlanDto.From(plan);
    }

    public async Task<PagedResult<DietitianUsageDto>> GetUsageAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        Paging.Ensure(page, pageSize);

        var total = await dietitians.CountAsync(cancellationToken);
        var pageItems = await dietitians.ListAsync(page, pageSize, cancellationToken);

        var items = new List<DietitianUsageDto>();
        foreach (var dietitian in pageItems)
        {
            var (subscription, usage) = await UsageOfAsync(dietitian, cancellationToken);
            items.Add(new DietitianUsageDto(dietitian.Id, dietitian.FullName, subscription?.Plan.Name, usage));
        }

        return new PagedResult<DietitianUsageDto>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        AuditLogFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        Paging.Ensure(page, pageSize);

        var total = await auditLogs.CountAsync(filter, cancellationToken);
        var logs = await auditLogs.ListAsync(filter, page, pageSize, cancellationToken);

        // Details alanı bilerek aktarılmaz: danışanın eski/yeni değerleri sağlık verisi olabilir.
        var items = logs
            .Select(log => new AuditLogDto(log.Id, log.CreatedAt, log.UserId, log.Action, log.EntityType, log.EntityId, log.IpAddress))
            .ToList();

        return new PagedResult<AuditLogDto>(items, page, pageSize, total);
    }

    /// <summary>Diyetisyenin güncel aboneliği ve kullanımı, ör. 8/10.</summary>
    private async Task<(Subscription? Subscription, UsageDto Usage)> UsageOfAsync(Dietitian dietitian, CancellationToken cancellationToken)
    {
        var subscription = await subscriptions.GetCurrentAsync(dietitian.Id, cancellationToken);
        var activeClients = await clients.CountActiveAsync(dietitian.Id, cancellationToken);

        var usage = subscription is null
            ? new UsageDto(activeClients, 0, false)
            : new UsageDto(activeClients, subscription.Plan.MaxClients, subscription.IsNearLimit(activeClients));

        return (subscription, usage);
    }
}
