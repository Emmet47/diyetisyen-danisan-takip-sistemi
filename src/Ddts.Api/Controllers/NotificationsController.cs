using System.ComponentModel.DataAnnotations;
using Ddts.Application.Common;
using Ddts.Application.Notifications;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Uygulama içi bildirimler (FR-J3). Herkes yalnızca kendi bildirimlerini görür.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize(Roles = $"{nameof(UserRole.Dietitian)},{nameof(UserRole.Client)}")]
[Produces("application/json")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>Bildirimler, en yenisi önce, sayfalı.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> List(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, Paging.MaxPageSize)] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Ok(await notificationService.ListAsync(page, pageSize, cancellationToken));

    /// <summary>Okundu işaretler.</summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await notificationService.MarkReadAsync(id, cancellationToken);
        return NoContent();
    }
}
