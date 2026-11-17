namespace Ddts.Application.Plans;

/// <summary>Gelen port: beslenme planları (E modülü, FR-E1–E5, Şekil 4.5, 4.6, 5.2, 5.4).</summary>
public interface IPlanService
{
    /// <summary>Builder ile taslak plan kurar (Director). Alerji kısıtlı besin ya da eksik öğün → 400 (tüm hatalar).</summary>
    Task<PlanCreatedDto> CreatePlanAsync(CreatePlanCommand command, CancellationToken cancellationToken = default);

    /// <summary>Yalnızca taslak plan düzenlenir (yayımlanmış/arşivlenmiş → 409).</summary>
    Task<PlanCreatedDto> UpdateDraftAsync(Guid planId, UpdatePlanCommand command, CancellationToken cancellationToken = default);

    /// <summary>Planı yayımlar; danışanın eski yayımlanmış planı arşivlenir, danışana bildirim gider (BR-03).</summary>
    Task PublishAsync(Guid planId, CancellationToken cancellationToken = default);

    Task ArchiveAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Plan, öğünler ve Composite toplamları (öğün, gün, plan).</summary>
    Task<PlanSummaryDto> GetPlanSummaryAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanListItemDto>> ListAsync(Guid? clientId, CancellationToken cancellationToken = default);

    /// <summary>Yalnızca taslak plan silinir (409).</summary>
    Task DeleteAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Giriş yapmış danışanın yayımlanmış planı (FR-E5, /api/me/plan); yoksa 404.</summary>
    Task<PlanSummaryDto> GetMyPlanAsync(CancellationToken cancellationToken = default);
}
