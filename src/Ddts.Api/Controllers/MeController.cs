using Ddts.Application.Clients;
using Ddts.Application.Measurements;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>
/// Danışanın mobil uygulaması (/api/me). Yalnızca danışan; kimlik token'dan gelir, adreste id yoktur:
/// danışan yalnızca kendi verisini görebilir.
/// </summary>
[ApiController]
[Route("api/me")]
[Authorize(Roles = nameof(UserRole.Client))]
[Produces("application/json")]
public sealed class MeController(IClientService clientService, IMeasurementService measurementService) : ControllerBase
{
    /// <summary>Kendi profili ve alerji/kısıt listesi.</summary>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ClientDetailDto>> Profile(CancellationToken cancellationToken) =>
        Ok(await clientService.GetMyProfileAsync(cancellationToken));

    /// <summary>Kendi ölçüm geçmişi ve grafik verisi: eskiden yeniye, farklar ve hedefe kalan kilo (FR-C4).</summary>
    [HttpGet("measurements")]
    [ProducesResponseType(typeof(MeasurementHistoryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeasurementHistoryDto>> Measurements(CancellationToken cancellationToken) =>
        Ok(await measurementService.ListMyMeasurementsAsync(cancellationToken));
}
