using Ddts.Application.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Satıştaki abonelik paketleri (L modülü). Kayıt ekranı için herkese açık.</summary>
[ApiController]
[Route("api/subscription-plans")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class SubscriptionPlansController(ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>Satıştaki paketler, danışan limitine göre sıralı (FR-L1).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> List(CancellationToken cancellationToken) =>
        Ok(await subscriptionService.ListPlansAsync(cancellationToken));
}
