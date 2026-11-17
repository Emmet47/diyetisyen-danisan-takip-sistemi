using System.ComponentModel.DataAnnotations;
using Ddts.Application.Plans;
using Ddts.Application.Templates;
using Ddts.Domain.Plans;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Plan şablonları (F modülü, Şekil 4.8 ve 5.3). Yalnızca diyetisyen; başka diyetisyenin şablonu 404.</summary>
[ApiController]
[Route("api/templates")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class TemplatesController(ITemplateService templateService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TemplateListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TemplateListItemDto>>> List(CancellationToken cancellationToken) =>
        Ok(await templateService.ListAsync(cancellationToken));

    /// <summary>Danışana bağlı olmayan, tekrar kullanılabilir şablon oluşturur (FR-F1).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create(TemplateRequest request, CancellationToken cancellationToken)
    {
        var id = await templateService.CreateAsync(request.ToCommand(), cancellationToken);
        return Created($"/api/templates/{id}", new IdResponse(id));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TemplateSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TemplateSummaryDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await templateService.GetAsync(id, cancellationToken));

    /// <summary>Şablonu baştan kaydeder. Daha önce kopyalanmış planlar etkilenmez (BR-07).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, TemplateRequest request, CancellationToken cancellationToken)
    {
        await templateService.UpdateAsync(id, request.ToCommand(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await templateService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Şablonun bağımsız kopyasını danışana taslak plan olarak oluşturur (FR-F2, Prototype). Danışanın kısıtlarına
    /// takılan besinler "warnings" alanında döner. Kopya düzenlenince şablon değişmez (FR-F3).
    /// </summary>
    /// <response code="201">Taslak plan oluştu.</response>
    /// <response code="409">Abonelik aktif değil.</response>
    [HttpPost("{id:guid}/copy-to-client")]
    [ProducesResponseType(typeof(PlanCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlanCreatedDto>> CopyToClient(Guid id, CopyTemplateRequest request, CancellationToken cancellationToken)
    {
        var result = await templateService.CopyToClientAsync(id, request.ClientId!.Value, request.Title, cancellationToken);
        return Created($"/api/plans/{result.PlanId}", result);
    }
}

/// <summary>Şablon gövdesi; meals plan sihirbazındakiyle aynı biçimde.</summary>
public sealed record TemplateRequest(
    [Required, StringLength(100)] string? Name,
    [StringLength(500)] string? Description,
    [Required] PlanType? PlanType,
    [Required] IReadOnlyList<MealInput>? Meals)
{
    public TemplateCommand ToCommand() => new(Name!, Description, PlanType!.Value, Meals!);
}

/// <summary>Kopyalanacak danışan ve yeni planın başlığı (boşsa şablon adı).</summary>
public sealed record CopyTemplateRequest([Required] Guid? ClientId, [StringLength(100)] string? Title);
