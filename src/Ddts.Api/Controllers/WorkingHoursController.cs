using System.ComponentModel.DataAnnotations;
using Ddts.Application.Appointments;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Diyetisyenin haftalık çalışma saatleri (FR-I1). Saatler Türkiye saati; gün 1 = Pazartesi … 7 = Pazar.</summary>
[ApiController]
[Route("api/working-hours")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class WorkingHoursController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkingHourDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkingHourDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await appointmentService.GetWorkingHoursAsync(cancellationToken));

    /// <summary>
    /// Haftayı baştan kaydeder: { "hours": [{ "weekday": 1, "startTime": "09:00:00", "endTime": "17:00:00" }] }.
    /// Her gün için tek aralık; listede olmayan gün çalışılmıyor sayılır.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Set(WorkingHoursRequest request, CancellationToken cancellationToken)
    {
        await appointmentService.SetWorkingHoursAsync(request.Hours!, cancellationToken);
        return NoContent();
    }
}

public sealed record WorkingHoursRequest([Required] IReadOnlyList<WorkingHourDto>? Hours);
