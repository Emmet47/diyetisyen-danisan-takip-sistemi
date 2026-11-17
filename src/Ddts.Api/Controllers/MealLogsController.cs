using Ddts.Application.MealLogs;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Öğün kayıtları ve fotoğraflar, diyetisyen tarafı (G modülü, Şekil 5.5). Danışan tarafı: /api/me.</summary>
[ApiController]
[Produces("application/json")]
public sealed class MealLogsController(IMealLogService mealLogService) : ControllerBase
{
    /// <summary>Planlanan ve gerçekleşen öğünler gün gün (FR-G3). Varsayılan son 7 gün, en fazla 31 gün.</summary>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpGet("api/clients/{clientId:guid}/meal-logs")]
    [Authorize(Roles = nameof(UserRole.Dietitian))]
    [ProducesResponseType(typeof(MealComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MealComparisonDto>> Comparison(
        Guid clientId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await mealLogService.GetComparisonAsync(clientId, from, to, cancellationToken));

    /// <summary>
    /// Öğün fotoğrafı için 10 dakika geçerli imzalı bağlantı (FR-G2). Danışan yalnızca kendi kaydı, diyetisyen yalnızca
    /// kendi danışanının kaydı için alabilir; başkasınınki 404 (IT-10).
    /// </summary>
    [HttpGet("api/meal-logs/{id:guid}/photo-url")]
    [Authorize(Roles = $"{nameof(UserRole.Dietitian)},{nameof(UserRole.Client)}")]
    [ProducesResponseType(typeof(PhotoUrlDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PhotoUrlDto>> PhotoUrl(Guid id, CancellationToken cancellationToken) =>
        Ok(await mealLogService.GetPhotoUrlAsync(id, cancellationToken));
}
