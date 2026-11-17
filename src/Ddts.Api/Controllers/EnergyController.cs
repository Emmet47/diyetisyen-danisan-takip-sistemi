using System.ComponentModel.DataAnnotations;
using Ddts.Application.Energy;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Enerji ihtiyacı hesabı (H modülü, Şekil 4.7). Yalnızca diyetisyen.</summary>
[ApiController]
[Route("api/energy")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class EnergyController(IEnergyService energyService) : ControllerBase
{
    /// <summary>
    /// BMH, günlük enerji ihtiyacı ve hedef enerji (FR-H1). formula: "MifflinStJeor" ya da "HarrisBenedict";
    /// activityFactor 1,2–1,9; adjustmentKcal isteğe bağlı (ör. −500). Danışanın son ölçümü gerekir.
    /// Sonuç tahminidir, profesyonel değerlendirme yerine geçmez (FR-H3).
    /// </summary>
    /// <response code="400">Bilinmeyen formül, sınır dışı katsayı ya da ölçüm yok.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(EnergyCalculationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnergyCalculationDto>> Calculate(EnergyRequest request, CancellationToken cancellationToken) =>
        Ok(await energyService.CalculateForClientAsync(
            request.ClientId!.Value, request.Formula!, request.ActivityFactor!.Value, request.AdjustmentKcal ?? 0, cancellationToken));
}

public sealed record EnergyRequest(
    [Required] Guid? ClientId,
    [Required, StringLength(30)] string? Formula,
    [Required] decimal? ActivityFactor,
    int? AdjustmentKcal);
