using System.ComponentModel.DataAnnotations;
using Ddts.Application.Plans;
using Ddts.Domain.Plans;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Beslenme planları (E modülü, Şekil 5.2 ve 5.4). Yalnızca diyetisyen; başka diyetisyenin planı 404.</summary>
[ApiController]
[Route("api/plans")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class PlansController(IPlanService planService) : ControllerBase
{
    /// <summary>Planlar, en yenisi önce; clientId verilirse yalnızca o danışanınkiler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlanListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlanListItemDto>>> List([FromQuery] Guid? clientId, CancellationToken cancellationToken) =>
        Ok(await planService.ListAsync(clientId, cancellationToken));

    /// <summary>
    /// Plan sihirbazı (FR-E1, Builder): danışan, başlık, tür (Daily/Weekly), isteğe bağlı enerji hedefi ve öğünler.
    /// Plan Taslak (Draft) olarak kaydedilir. Alerji listesindeki besin ya da eksik öğün → 400, hatalar "errors" alanında.
    /// "Sevmiyor" listesindeki besinler yanıtta uyarı (warnings) olarak döner.
    /// </summary>
    /// <response code="201">Taslak plan oluştu; toplamlar ve uyarılar yanıtta.</response>
    /// <response code="409">Abonelik aktif değil.</response>
    [HttpPost]
    [ProducesResponseType(typeof(PlanCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanCreatedDto>> Create(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await planService.CreatePlanAsync(
            new CreatePlanCommand(
                request.ClientId!.Value, request.Title!, request.PlanType!.Value, request.TargetKcal, request.EnergyFormula, request.Meals!),
            cancellationToken);

        return Created($"/api/plans/{result.PlanId}", result);
    }

    /// <summary>Plan, öğünler ve toplamlar (öğün, gün, plan; FR-H2).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlanSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanSummaryDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await planService.GetPlanSummaryAsync(id, cancellationToken));

    /// <summary>Taslak planın içeriğini baştan kaydeder. Yayımlanmış ya da arşivlenmiş plan değiştirilemez (409).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PlanCreatedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanCreatedDto>> Update(Guid id, UpdatePlanRequest request, CancellationToken cancellationToken) =>
        Ok(await planService.UpdateDraftAsync(
            id,
            new UpdatePlanCommand(request.Title!, request.PlanType!.Value, request.TargetKcal, request.EnergyFormula, request.Meals!),
            cancellationToken));

    /// <summary>Taslak planı siler (yalnızca taslak, 409).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await planService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Planı yayımlar (FR-E4): danışanın eski yayımlanmış planı arşivlenir, danışana "Yeni planın hazır" bildirimi gider.
    /// Alerjen besin içeren plan yayımlanamaz (400).
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(PlanSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanSummaryDto>> Publish(Guid id, CancellationToken cancellationToken)
    {
        await planService.PublishAsync(id, cancellationToken);
        return Ok(await planService.GetPlanSummaryAsync(id, cancellationToken));
    }

    /// <summary>Yayımlanmış planı arşivler (geri açılmaz).</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(PlanSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanSummaryDto>> Archive(Guid id, CancellationToken cancellationToken)
    {
        await planService.ArchiveAsync(id, cancellationToken);
        return Ok(await planService.GetPlanSummaryAsync(id, cancellationToken));
    }
}

/// <summary>
/// Plan sihirbazı gövdesi. meals: [{ dayNo, name, time: "08:00:00", items: [{ foodItemId, portionCount,
/// alternatives: [{ foodItemId, portionCount }] }] }]. Haftalık planda dayNo 1 = Pazartesi … 7 = Pazar.
/// </summary>
public sealed record CreatePlanRequest(
    [Required] Guid? ClientId,
    [Required, StringLength(100)] string? Title,
    [Required] PlanType? PlanType,
    int? TargetKcal,
    [StringLength(30)] string? EnergyFormula,
    [Required] IReadOnlyList<MealInput>? Meals);

/// <summary>Taslak plan düzenleme gövdesi (danışan değişmez).</summary>
public sealed record UpdatePlanRequest(
    [Required, StringLength(100)] string? Title,
    [Required] PlanType? PlanType,
    int? TargetKcal,
    [StringLength(30)] string? EnergyFormula,
    [Required] IReadOnlyList<MealInput>? Meals);
