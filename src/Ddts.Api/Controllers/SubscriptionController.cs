using System.ComponentModel.DataAnnotations;
using Ddts.Application.Subscriptions;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Giriş yapan diyetisyenin aboneliği (L modülü, UC-04). Yalnızca diyetisyen; başka rol 403 alır.</summary>
[ApiController]
[Route("api/subscription")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class SubscriptionController(ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>Paket, durum (Active / Expired / Cancelled), bitiş tarihi ve kullanım, ör. 8/10 (FR-L4).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionStatusDto>> Get(CancellationToken cancellationToken) =>
        Ok(await subscriptionService.GetStatusAsync(cancellationToken));

    /// <summary>Paket yükseltme ya da düşürme (FR-L3). Yeni paket bugünden başlar, eskisi iptal edilir.</summary>
    /// <response code="400">Paket satışta değil ya da zaten bu paket kullanılıyor.</response>
    /// <response code="409">Aktif danışan sayısı yeni paketin limitinden fazla (BR-02).</response>
    [HttpPost("change")]
    [ProducesResponseType(typeof(SubscriptionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionStatusDto>> Change(ChangePlanRequest request, CancellationToken cancellationToken) =>
        Ok(await subscriptionService.ChangePlanAsync(request.SubscriptionPlanId, cancellationToken));

    /// <summary>Ödeme simülasyonuyla aboneliği bir ay uzatır (FR-L4).</summary>
    /// <response code="400">İptal edilmiş abonelik yenilenemez.</response>
    [HttpPost("renew")]
    [ProducesResponseType(typeof(SubscriptionStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionStatusDto>> Renew(CancellationToken cancellationToken) =>
        Ok(await subscriptionService.RenewAsync(cancellationToken));
}

/// <param name="SubscriptionPlanId">GET /api/subscription-plans listesinden yeni paket.</param>
public sealed record ChangePlanRequest([Required] Guid SubscriptionPlanId);
