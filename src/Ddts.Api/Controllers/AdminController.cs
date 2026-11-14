using System.ComponentModel.DataAnnotations;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Admin;
using Ddts.Application.Common;
using Ddts.Application.Subscriptions;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>
/// Platform yöneticisi (UC-05…UC-07). Yalnızca yönetici; başka rol 403 alır. Yönetici danışan verisini
/// göremez: yalnızca hesaplar, paketler, kullanım sayıları ve ayrıntısız denetim kayıtları (rol-yetki matrisi 8.4).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = nameof(UserRole.Admin))]
[Produces("application/json")]
public sealed class AdminController(IAdminService adminService) : ControllerBase
{
    /// <summary>Diyetisyenler, paketleri ve kullanımları (FR-L5); ada göre sıralı, sayfalı.</summary>
    /// <response code="400">Sayfa ya da sayfa boyutu geçersiz (sayfa ≥ 1, boyut 1–100).</response>
    [HttpGet("dietitians")]
    [ProducesResponseType(typeof(PagedResult<DietitianAdminDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DietitianAdminDto>>> ListDietitians(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, Paging.MaxPageSize)] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Ok(await adminService.ListDietitiansAsync(page, pageSize, cancellationToken));

    /// <summary>Diyetisyen hesabını açar (FR-L5).</summary>
    /// <response code="204">Hesap açıldı.</response>
    /// <response code="404">Diyetisyen yok.</response>
    /// <response code="409">Hesap zaten aktif.</response>
    [HttpPost("dietitians/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateDietitian(Guid id, CancellationToken cancellationToken)
    {
        await adminService.SetDietitianActiveAsync(id, isActive: true, cancellationToken);
        return NoContent();
    }

    /// <summary>Diyetisyen hesabını kapatır: giriş yapamaz, elindeki token da geçmez (FR-L5).</summary>
    /// <response code="204">Hesap kapatıldı.</response>
    /// <response code="404">Diyetisyen yok.</response>
    /// <response code="409">Hesap zaten pasif.</response>
    [HttpPost("dietitians/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateDietitian(Guid id, CancellationToken cancellationToken)
    {
        await adminService.SetDietitianActiveAsync(id, isActive: false, cancellationToken);
        return NoContent();
    }

    /// <summary>Satıştan kaldırılanlar dahil bütün paketler (FR-L1).</summary>
    [HttpGet("subscription-plans")]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionPlanDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanDto>>> ListPackages(CancellationToken cancellationToken) =>
        Ok(await adminService.ListPackagesAsync(cancellationToken));

    /// <summary>Yeni paket tanımlar (FR-L1).</summary>
    /// <response code="201">Paket oluştu.</response>
    /// <response code="400">Hatalı alan (ör. danışan limiti 0).</response>
    /// <response code="409">Bu adla bir paket zaten var.</response>
    [HttpPost("subscription-plans")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionPlanDto>> CreatePackage(SavePackageRequest request, CancellationToken cancellationToken)
    {
        var plan = await adminService.SavePackageAsync(null, request.ToCommand(), cancellationToken);
        return Created($"/api/admin/subscription-plans/{plan.Id}", plan);
    }

    /// <summary>Paketi düzenler ya da satıştan kaldırır (isActive = false). Paketi kullananların aboneliği sürer.</summary>
    /// <response code="400">Hatalı alan.</response>
    /// <response code="404">Paket yok.</response>
    /// <response code="409">Bu adla başka bir paket var.</response>
    [HttpPut("subscription-plans/{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubscriptionPlanDto>> UpdatePackage(Guid id, SavePackageRequest request, CancellationToken cancellationToken) =>
        Ok(await adminService.SavePackageAsync(id, request.ToCommand(), cancellationToken));

    /// <summary>Diyetisyen başına kullanım, yalnızca sayı olarak (ör. 8/10); sayfalı.</summary>
    [HttpGet("usage")]
    [ProducesResponseType(typeof(PagedResult<DietitianUsageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DietitianUsageDto>>> Usage(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, Paging.MaxPageSize)] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Ok(await adminService.GetUsageAsync(page, pageSize, cancellationToken));

    /// <summary>Denetim kayıtları, ayrıntı alanı (eski/yeni değerler) olmadan; en yenisi önce, sayfalı (FR-L5, FR-A5).</summary>
    /// <param name="from">Bu günden itibaren (yyyy-aa-gg, UTC).</param>
    /// <param name="to">Bu gün dahil (yyyy-aa-gg, UTC).</param>
    /// <param name="userId">İşlemi yapan kullanıcı.</param>
    /// <param name="action">İşlem adı, ör. LoginFailed, ClientAdded, SubscriptionChanged.</param>
    /// <param name="page">Sayfa (1'den başlar).</param>
    /// <param name="pageSize">Sayfa boyutu (1–100).</param>
    /// <param name="cancellationToken">İstek iptal edilirse sorgu durur.</param>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> AuditLogs(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? userId,
        [FromQuery, StringLength(50)] string? action,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, Paging.MaxPageSize)] int pageSize = Paging.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Ok(await adminService.GetAuditLogsAsync(new AuditLogFilter(from, to, userId, action), page, pageSize, cancellationToken));
}

/// <summary>Paket formu (FR-L1).</summary>
/// <param name="Name">Paket adı (tekil, en fazla 50 karakter).</param>
/// <param name="MaxClients">Aynı anda en fazla kaç aktif danışan (0'dan büyük).</param>
/// <param name="MonthlyPrice">Aylık ücret, TL (0–999999.99).</param>
/// <param name="IsActive">Satışta mı? false: yeni kayıtta seçilemez.</param>
public sealed record SavePackageRequest(
    [Required, StringLength(50)] string Name,
    int MaxClients,
    decimal MonthlyPrice,
    bool IsActive = true)
{
    public SavePackageCommand ToCommand() => new(Name, MaxClients, MonthlyPrice, IsActive);
}
