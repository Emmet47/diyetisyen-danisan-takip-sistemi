using System.ComponentModel.DataAnnotations;
using Ddts.Application.Clients;
using Ddts.Application.Subscriptions;
using Ddts.Domain.Clients;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>
/// Diyetisyenin danışanları (B modülü, UC-08…UC-11). Yalnızca diyetisyen; başka rol 403 alır.
/// Başka diyetisyenin danışanına giden her istek 404 döner: kaydın varlığı bile belli edilmez (Şekil 5.9).
/// </summary>
[ApiController]
[Route("api/clients")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class ClientsController(IClientService clientService, ISubscriptionService subscriptionService) : ControllerBase
{
    /// <summary>Kendi danışanları, ada göre sıralı. <c>?active=true</c> yalnızca aktifleri getirir.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientSummaryDto>>> List([FromQuery] bool active, CancellationToken cancellationToken) =>
        Ok(await clientService.ListAsync(active, cancellationToken));

    /// <summary>Danışan ekler; paket limiti kontrol edilir ve danışana davet e-postası gider (FR-B1, FR-L2).</summary>
    /// <remarks>
    /// "Açık rıza alındı" onayı (kvkkConsentGiven) zorunlu. Davet bağlantısı 30 dakika geçerli; süresi
    /// dolarsa danışan "Parolamı unuttum" ile yeni bağlantı alır. Yanıttaki usage, paket kullanımını
    /// gösterir (nearLimit = limitin %80'i doldu).
    /// </remarks>
    /// <response code="201">Danışan eklendi.</response>
    /// <response code="400">Eksik/hatalı alan ya da açık rıza işaretlenmemiş.</response>
    /// <response code="409">Paket limiti dolu ya da abonelik aktif değil; ya da e-posta kayıtlı.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AddClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AddClientResponse>> Add(AddClientRequest request, CancellationToken cancellationToken)
    {
        var clientId = await clientService.AddClientAsync(
            new AddClientCommand(
                request.FullName,
                request.Email,
                request.Phone,
                request.BirthDate!.Value,
                request.Sex!.Value,
                request.GoalType!.Value,
                request.TargetWeightKg,
                request.Preferences,
                request.AllergyNotes,
                request.KvkkConsentGiven),
            cancellationToken);

        var subscription = await subscriptionService.GetStatusAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = clientId }, new AddClientResponse(clientId, subscription.Usage));
    }

    /// <summary>Danışan profili ve alerji/kısıt listesi.</summary>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDetailDto>> Get(Guid id, CancellationToken cancellationToken) =>
        Ok(await clientService.GetAsync(id, cancellationToken));

    /// <summary>Profili günceller; değişen alanlar eski/yeni değerleriyle geçmişe yazılır (FR-B3).</summary>
    /// <remarks>Doğum tarihi, cinsiyet ve e-posta bu uçla değişmez.</remarks>
    /// <response code="204">Güncellendi.</response>
    /// <response code="400">Hatalı alan.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, UpdateClientRequest request, CancellationToken cancellationToken)
    {
        await clientService.UpdateClientAsync(
            id,
            new ClientProfile(
                request.FullName,
                request.Phone,
                request.GoalType!.Value,
                request.TargetWeightKg,
                request.Preferences,
                request.AllergyNotes),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Danışanı ve bütün kayıtlarını kalıcı siler (FR-B4, KVKK).</summary>
    /// <remarks>Ölçüm, plan, öğün kaydı, randevu ve bildirimler de silinir; denetim kayıtlarında yalnızca işlem bilgisi kalır.</remarks>
    /// <response code="204">Silindi.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await clientService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Alerji/kısıt listesini verilen listeyle değiştirir (FR-B2).</summary>
    /// <remarks>Allergy: besin plana eklenemez. Dislike: plana eklenince uyarı verir. Boş liste kısıtları temizler.</remarks>
    /// <response code="204">Liste kaydedildi.</response>
    /// <response code="400">Besin bulunamadı ya da aynı besin iki kez yazıldı.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpPut("{id:guid}/restrictions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetRestrictions(Guid id, SetRestrictionsRequest request, CancellationToken cancellationToken)
    {
        var restrictions = request.Restrictions!
            .Select(r => new RestrictionInput(r.FoodItemId!.Value, r.Type!.Value))
            .ToList();

        await clientService.SetRestrictionsAsync(id, restrictions, cancellationToken);
        return NoContent();
    }

    /// <summary>Değişiklik geçmişi: işlem, zaman, yapan kullanıcı ve eski/yeni değerler; en yenisi önce (FR-B3).</summary>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    [HttpGet("{id:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<ClientHistoryEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ClientHistoryEntryDto>>> History(Guid id, CancellationToken cancellationToken) =>
        Ok(await clientService.GetHistoryAsync(id, cancellationToken));

    /// <summary>Pasife alır: paket sayımına girmez, danışan giriş yapamaz, veriler durur (FR-B4).</summary>
    /// <response code="204">Pasife alındı.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    /// <response code="409">Danışan zaten pasif.</response>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await clientService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Pasif danışanı yeniden aktifleştirir; paket limiti yeniden kontrol edilir (FR-B4, FR-L2).</summary>
    /// <response code="204">Aktifleştirildi.</response>
    /// <response code="404">Danışan yok ya da başka diyetisyenin danışanı.</response>
    /// <response code="409">Paket limiti dolu, abonelik aktif değil ya da danışan zaten aktif.</response>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await clientService.ActivateAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>Yeni danışan (docs/api.md 8.3, örnek 2).</summary>
/// <param name="FullName">Ad soyad.</param>
/// <param name="Email">Danışanın giriş e-postası; davet bu adrese gider.</param>
/// <param name="Phone">İsteğe bağlı telefon.</param>
/// <param name="BirthDate">Doğum tarihi (yyyy-aa-gg).</param>
/// <param name="Sex">Female ya da Male.</param>
/// <param name="GoalType">LoseWeight, Maintain ya da GainWeight.</param>
/// <param name="TargetWeightKg">İsteğe bağlı hedef kilo (20–350).</param>
/// <param name="Preferences">Beslenme tercihleri (serbest metin).</param>
/// <param name="AllergyNotes">Alerji notu (serbest metin).</param>
/// <param name="KvkkConsentGiven">Danışandan açık rıza alındı mı? true olmalı (KVKK).</param>
public sealed record AddClientRequest(
    [Required, StringLength(100)] string FullName,
    [Required, EmailAddress, StringLength(254)] string Email,
    [StringLength(20)] string? Phone,
    [Required] DateOnly? BirthDate,
    [Required] Sex? Sex,
    [Required] GoalType? GoalType,
    decimal? TargetWeightKg,
    [StringLength(500)] string? Preferences,
    [StringLength(500)] string? AllergyNotes,
    bool KvkkConsentGiven);

/// <summary>Eklenen danışanın kimliği ve paketin yeni kullanımı, ör. 9/10.</summary>
public sealed record AddClientResponse(Guid Id, UsageDto Usage);

/// <summary>Diyetisyenin değiştirebildiği profil alanları.</summary>
public sealed record UpdateClientRequest(
    [Required, StringLength(100)] string FullName,
    [StringLength(20)] string? Phone,
    [Required] GoalType? GoalType,
    decimal? TargetWeightKg,
    [StringLength(500)] string? Preferences,
    [StringLength(500)] string? AllergyNotes);

/// <summary>Kısıt listesinin tamamı; listede olmayan besin listeden çıkar.</summary>
public sealed record SetRestrictionsRequest([Required] IReadOnlyList<RestrictionRequest>? Restrictions);

/// <param name="FoodItemId">Besin (GET /api/foods).</param>
/// <param name="Type">Allergy ya da Dislike.</param>
public sealed record RestrictionRequest([Required] Guid? FoodItemId, [Required] RestrictionType? Type);
