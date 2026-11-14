using System.ComponentModel.DataAnnotations;
using Ddts.Application.Measurements;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>
/// Danışan ölçümleri (C modülü, UC-11, UC-12, Şekil 5.6). Yalnızca diyetisyen; başka rol 403 alır.
/// Başka diyetisyenin danışanına giden her istek 404 döner (Şekil 5.9).
/// </summary>
[ApiController]
[Route("api/clients/{clientId:guid}/measurements")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class MeasurementsController(IMeasurementService measurementService) : ControllerBase
{
    /// <summary>Ölçüm geçmişi: eskiden yeniye, her ölçümde öncekine göre fark ve hedefe kalan kilo (FR-C2, FR-C3).</summary>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpGet]
    [ProducesResponseType(typeof(MeasurementHistoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeasurementHistoryDto>> List(Guid clientId, CancellationToken cancellationToken) =>
        Ok(await measurementService.ListAsync(clientId, cancellationToken));

    /// <summary>Yeni ölçüm ekler (FR-C1). Aynı gün için tek ölçüm girilir.</summary>
    /// <remarks>Kilo 20–350 kg, boy 100–250 cm, bel ve kalça 30–250 cm, yağ oranı %2–75.</remarks>
    /// <response code="201">Ölçüm eklendi.</response>
    /// <response code="400">Eksik ya da sınır dışı değer.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    /// <response code="409">O gün için zaten ölçüm var; mevcut ölçüm düzenlenmeli.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MeasurementCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MeasurementCreatedResponse>> Add(
        Guid clientId, MeasurementRequest request, CancellationToken cancellationToken)
    {
        var measurementId = await measurementService.AddAsync(clientId, request.ToCommand(), cancellationToken);

        return Created(
            $"/api/clients/{clientId}/measurements/{measurementId}",
            new MeasurementCreatedResponse(measurementId));
    }

    /// <summary>Yanlış girilmiş ölçümü düzeltir; tarih de değiştirilebilir.</summary>
    /// <response code="204">Düzeltildi.</response>
    /// <response code="400">Eksik ya da sınır dışı değer.</response>
    /// <response code="404">Danışan ya da ölçüm yok, ya da başka diyetisyenin danışanı.</response>
    /// <response code="409">Yeni tarihte başka bir ölçüm var.</response>
    [HttpPut("{measurementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid clientId, Guid measurementId, MeasurementRequest request, CancellationToken cancellationToken)
    {
        await measurementService.UpdateAsync(clientId, measurementId, request.ToCommand(), cancellationToken);
        return NoContent();
    }

    /// <summary>Ölçümü siler.</summary>
    /// <response code="204">Silindi.</response>
    /// <response code="404">Danışan ya da ölçüm yok, ya da başka diyetisyenin danışanı.</response>
    [HttpDelete("{measurementId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid clientId, Guid measurementId, CancellationToken cancellationToken)
    {
        await measurementService.DeleteAsync(clientId, measurementId, cancellationToken);
        return NoContent();
    }
}

/// <summary>Ölçüm formu (FR-C1). Ondalık ayırıcı nokta: 72.4.</summary>
/// <param name="MeasuredOn">Ölçüm günü (yyyy-aa-gg).</param>
/// <param name="WeightKg">Kilo (kg), zorunlu.</param>
/// <param name="HeightCm">Boy (cm), zorunlu.</param>
/// <param name="WaistCm">Bel çevresi (cm), zorunlu.</param>
/// <param name="HipCm">Kalça çevresi (cm), isteğe bağlı.</param>
/// <param name="BodyFatPercent">Vücut yağ oranı (%), isteğe bağlı.</param>
/// <param name="Note">İsteğe bağlı not.</param>
public sealed record MeasurementRequest(
    [Required] DateOnly? MeasuredOn,
    [Required] decimal? WeightKg,
    [Required] decimal? HeightCm,
    [Required] decimal? WaistCm,
    decimal? HipCm,
    decimal? BodyFatPercent,
    [StringLength(300)] string? Note)
{
    public MeasurementCommand ToCommand() => new(
        MeasuredOn!.Value, WeightKg!.Value, HeightCm!.Value, WaistCm!.Value, HipCm, BodyFatPercent, Note);
}

/// <summary>Eklenen ölçümün kimliği.</summary>
public sealed record MeasurementCreatedResponse(Guid Id);
