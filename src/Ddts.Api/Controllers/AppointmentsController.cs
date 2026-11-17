using System.ComponentModel.DataAnnotations;
using Ddts.Application.Appointments;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>
/// Diyetisyenin randevuları (I modülü, Şekil 5.7). Zamanlar UTC gönderilir/döner (ör. 2026-12-01T07:00:00Z = 10:00 TSİ).
/// Danışanın talep, erteleme ve iptali /api/me altındadır.
/// </summary>
[ApiController]
[Route("api/appointments")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    /// <summary>Takvim: [from, to) arasında başlayan randevular. Varsayılan: bugünden itibaren 30 gün.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> List(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken cancellationToken) =>
        Ok(await appointmentService.ListAsync(from, to, cancellationToken));

    /// <summary>Doğrudan onaylı randevu oluşturur (FR-I2). Slot doluysa 409 "Bu saat az önce doldu".</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.CreateConfirmedAsync(request.ClientId!.Value, request.StartsAt!.Value, cancellationToken);
        return Created($"/api/appointments/{appointment.Id}", appointment);
    }

    /// <summary>Talebi onaylar (Talep Edildi → Onaylandı); danışana bildirim gider.</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Confirm(Guid id, CancellationToken cancellationToken) =>
        Ok(await appointmentService.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await appointmentService.CancelAsync(id, cancellationToken));

    /// <summary>Erteler; diyetisyen ertelerse randevu onaylı kalır.</summary>
    [HttpPost("{id:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Reschedule(Guid id, RescheduleRequest request, CancellationToken cancellationToken) =>
        Ok(await appointmentService.RescheduleAsync(id, request.StartsAt!.Value, cancellationToken));

    /// <summary>Görüşmeden sonra "Tamamlandı" (yalnızca onaylı ve saati geçmiş randevu).</summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Complete(Guid id, CancellationToken cancellationToken) =>
        Ok(await appointmentService.CompleteAsync(id, cancellationToken));
}

public sealed record CreateAppointmentRequest([Required] Guid? ClientId, [Required] DateTime? StartsAt);

/// <summary>Yeni başlangıç zamanı (UTC ya da +03:00 ile).</summary>
public sealed record RescheduleRequest([Required] DateTime? StartsAt);
