using System.ComponentModel.DataAnnotations;
using Ddts.Application.Appointments;
using Ddts.Application.Clients;
using Ddts.Application.MealLogs;
using Ddts.Application.Measurements;
using Ddts.Application.Notifications;
using Ddts.Application.Plans;
using Ddts.Domain.MealLogs;
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
public sealed class MeController(
    IClientService clientService,
    IMeasurementService measurementService,
    IPlanService planService,
    IMealLogService mealLogService,
    IAppointmentService appointmentService,
    INotificationService notificationService) : ControllerBase
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

    /// <summary>Yayımlanmış planı, öğünler ve toplamlarla (FR-E5). Henüz yoksa 404.</summary>
    [HttpGet("plan")]
    [ProducesResponseType(typeof(PlanSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlanSummaryDto>> Plan(CancellationToken cancellationToken) =>
        Ok(await planService.GetMyPlanAsync(cancellationToken));

    /// <summary>"Bugün" ekranı: bugünün planlı öğünleri ve kayıt durumları (Şekil 5.5).</summary>
    [HttpGet("meals/today")]
    [ProducesResponseType(typeof(TodayDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TodayDto>> Today(CancellationToken cancellationToken) =>
        Ok(await mealLogService.GetTodayAsync(cancellationToken));

    /// <summary>Kendi öğün kayıtları (varsayılan son 7 gün, en fazla 31 gün).</summary>
    [HttpGet("meal-logs")]
    [ProducesResponseType(typeof(IReadOnlyList<MealLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MealLogDto>>> MealLogs(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken) =>
        Ok(await mealLogService.ListMyLogsAsync(from, to, cancellationToken));

    /// <summary>
    /// Öğünü işaretler (FR-G1): status "Eaten" (yedim), "Partial" (kısmen), "Skipped" (yemedim). Bugün ya da geçmiş
    /// bir gün; aynı öğün aynı gün için bir kez (409, sonra güncellenir).
    /// </summary>
    [HttpPost("meal-logs")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<IdResponse>> LogMeal(LogMealRequest request, CancellationToken cancellationToken)
    {
        var id = await mealLogService.LogMealAsync(
            new LogMealCommand(request.MealId!.Value, request.LogDate!.Value, request.Status!.Value, request.Note),
            cancellationToken);
        return Created($"/api/me/meal-logs/{id}", new IdResponse(id));
    }

    /// <summary>Kaydın durumunu ve notunu günceller.</summary>
    [HttpPut("meal-logs/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMealLog(Guid id, UpdateMealLogRequest request, CancellationToken cancellationToken)
    {
        await mealLogService.UpdateLogAsync(id, new UpdateMealLogCommand(request.Status!.Value, request.Note), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Öğün fotoğrafı yükler (multipart/form-data, alan adı "file"). Yalnızca JPEG/PNG (dosyanın içine bakılır),
    /// en fazla 5 MB, kayıt başına bir fotoğraf (FR-G2, BR-10).
    /// </summary>
    /// <response code="400">Geçersiz dosya.</response>
    /// <response code="409">Kayıtta zaten fotoğraf var.</response>
    [HttpPost("meal-logs/{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadPhoto(Guid id, [Required] IFormFile? file, CancellationToken cancellationToken)
    {
        await using var stream = file!.OpenReadStream();
        await mealLogService.AttachPhotoAsync(id, stream, cancellationToken);
        return Ok();
    }

    /// <summary>Diyetisyenin o günkü boş 30 dakikalık slotları (UTC), ör. ?date=2026-12-01 (FR-I1).</summary>
    [HttpGet("available-slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SlotDto>>> AvailableSlots([FromQuery, Required] DateOnly? date, CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetFreeSlotsAsync(date!.Value, cancellationToken));

    /// <summary>Kendi randevuları, en yenisi önce.</summary>
    [HttpGet("appointments")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> Appointments(CancellationToken cancellationToken) =>
        Ok(await appointmentService.ListMyAsync(cancellationToken));

    /// <summary>Randevu talep eder (Talep Edildi). Slot az önce dolduysa 409 "Bu saat az önce doldu".</summary>
    [HttpPost("appointments")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> RequestAppointment(RequestAppointmentRequest request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentService.RequestAsync(request.StartsAt!.Value, cancellationToken);
        return Created($"/api/me/appointments/{appointment.Id}", appointment);
    }

    /// <summary>Erteler: randevu yeniden Talep Edildi olur, diyetisyenin onayını bekler (BR-09).</summary>
    [HttpPost("appointments/{id:guid}/reschedule")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> RescheduleAppointment(Guid id, RescheduleRequest request, CancellationToken cancellationToken) =>
        Ok(await appointmentService.RescheduleAsync(id, request.StartsAt!.Value, cancellationToken));

    [HttpPost("appointments/{id:guid}/cancel")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> CancelAppointment(Guid id, CancellationToken cancellationToken) =>
        Ok(await appointmentService.CancelAsync(id, cancellationToken));

    /// <summary>Bildirim izinleri (FR-J2).</summary>
    [HttpGet("notification-settings")]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> NotificationSettings(CancellationToken cancellationToken) =>
        Ok(await notificationService.GetSettingsAsync(cancellationToken));

    /// <summary>Dört bildirim türünü açar/kapatır; kapalı türde bildirim üretilmez (FR-J2).</summary>
    [HttpPut("notification-settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateNotificationSettings(NotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        await notificationService.UpdateSettingsAsync(
            new NotificationSettingsDto(request.Appointments!.Value, request.MealReminders!.Value, request.Measurements!.Value, request.Plans!.Value),
            cancellationToken);
        return NoContent();
    }

    /// <summary>Telefonun FCM anahtarını kaydeder; boş gönderilirse siler (push kapanır).</summary>
    [HttpPut("push-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PushToken(PushTokenRequest request, CancellationToken cancellationToken)
    {
        await notificationService.UpdatePushTokenAsync(request.PushToken, cancellationToken);
        return NoContent();
    }
}

/// <summary>Öğün kaydı: mealId planındaki öğün, logDate yyyy-aa-gg, status Eaten/Partial/Skipped.</summary>
public sealed record LogMealRequest(
    [Required] Guid? MealId,
    [Required] DateOnly? LogDate,
    [Required] MealLogStatus? Status,
    [StringLength(500)] string? Note);

public sealed record UpdateMealLogRequest([Required] MealLogStatus? Status, [StringLength(500)] string? Note);

/// <summary>İstenen slotun başlangıcı (UTC ya da +03:00 ile).</summary>
public sealed record RequestAppointmentRequest([Required] DateTime? StartsAt);

public sealed record NotificationSettingsRequest(
    [Required] bool? Appointments,
    [Required] bool? MealReminders,
    [Required] bool? Measurements,
    [Required] bool? Plans);

public sealed record PushTokenRequest([StringLength(300)] string? PushToken);
