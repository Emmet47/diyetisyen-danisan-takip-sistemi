using Ddts.Application.Reports;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>İlerleme paneli ve PDF raporu (K modülü, Şekil 5.6). Yalnızca diyetisyen; başka diyetisyenin danışanı 404.</summary>
[ApiController]
[Route("api/clients/{clientId:guid}")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Panel verisi (FR-K1, FR-K3): dönemin ölçümleri ve farkları, hedefe kalan kilo, plan uyumu ve kayıt sıklığı.
    /// Varsayılan dönem son 30 gün, en fazla 366 gün. Grafikleri web çizer.
    /// </summary>
    [HttpGet("progress")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ProgressReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgressReportDto>> Progress(
        Guid clientId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await reportService.GetProgressAsync(clientId, from, to, cancellationToken));

    /// <summary>Aynı dönemin PDF raporu (FR-K2).</summary>
    [HttpGet("report.pdf")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pdf(
        Guid clientId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var file = await reportService.RenderPdfAsync(clientId, from, to, cancellationToken);
        return File(file.Content, "application/pdf", file.FileName);
    }
}
