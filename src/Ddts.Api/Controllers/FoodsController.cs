using System.ComponentModel.DataAnnotations;
using Ddts.Application.Foods;
using Ddts.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ddts.Api.Controllers;

/// <summary>Besin veritabanı (D modülü, Şekil 4.9 ve 5.8). Yalnızca diyetisyen.</summary>
[ApiController]
[Route("api/foods")]
[Authorize(Roles = nameof(UserRole.Dietitian))]
[Produces("application/json")]
public sealed class FoodsController(IFoodService foodService, IFoodSearchService foodSearchService) : ControllerBase
{
    /// <summary>
    /// Besin arama (FR-D1). Yerel kayıtlar (ortak + kendi) her zaman aranır; external=true ise dış kaynaklarda
    /// (PlateRank, USDA) da aranır. source=Yerel, source=PlateRank ya da source=USDA ile kaynağa göre süzülür.
    /// USDA'ya giden sorgu İngilizceye, gelen adlar Türkçeye çevrilir (DeepL). Bir dış kaynak çalışmazsa yine
    /// 200 döner: externalUnavailable=true ve uyarı mesajıyla diğer sonuçlar (FR-D4).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FoodSearchResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<FoodSearchResult>> Search(
        [FromQuery] string? q,
        [FromQuery] string? source,
        [FromQuery] bool external = false,
        CancellationToken cancellationToken = default) =>
        Ok(await foodSearchService.SearchAsync(q, source, external, cancellationToken));

    /// <summary>Yerel besin ekler (FR-D2). Değerler 100 g içindir.</summary>
    /// <response code="201">Eklendi.</response>
    /// <response code="400">Eksik ya da sınır dışı değer (makro toplamı 100 g'ı geçemez).</response>
    [HttpPost]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IdResponse>> Create(FoodRequest request, CancellationToken cancellationToken)
    {
        var id = await foodService.CreateAsync(request.ToCommand(), cancellationToken);
        return Created($"/api/foods/{id}", new IdResponse(id));
    }

    /// <summary>Kendi besin kaydını düzenler. Ortak kayıtlar düzenlenemez (403).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, FoodRequest request, CancellationToken cancellationToken)
    {
        await foodService.UpdateAsync(id, request.ToCommand(), cancellationToken);
        return NoContent();
    }

    /// <summary>Kendi besin kaydını siler. Planda ya da şablonda kullanılan besin silinemez (409).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await foodService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Dış kaynaktaki (PlateRank ya da USDA) besini yerel veritabanına aktarır (FR-D3). Türkçe ad ve porsiyon
    /// verilebilir. Kaynak porsiyon gramını vermiyorsa (valuesPerPortion) portionGrams zorunludur; değerler
    /// 100 g'a çevrilir. Daha önce aktarıldıysa mevcut kaydın kimliği döner.
    /// </summary>
    /// <response code="400">Porsiyon gramı gerekirken girilmedi, değerler sınır dışı ya da kaynak bilinmiyor.</response>
    /// <response code="503">Dış kaynağa şu an ulaşılamıyor.</response>
    [HttpPost("import")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<IdResponse>> Import(ImportFoodRequest request, CancellationToken cancellationToken) =>
        Ok(new IdResponse(await foodSearchService.ImportAsync(
            new ImportFoodCommand(request.ExternalId!, request.Name, request.PortionName, request.PortionGrams, request.Source),
            cancellationToken)));
}

/// <summary>Yerel besin formu (100 g için değerler). Ondalık ayırıcı nokta: 13.2.</summary>
public sealed record FoodRequest(
    [Required, StringLength(150)] string? Name,
    [Required, StringLength(50)] string? PortionName,
    [Required] decimal? PortionGrams,
    [Required] decimal? EnergyKcalPer100g,
    [Required] decimal? ProteinPer100g,
    [Required] decimal? CarbsPer100g,
    [Required] decimal? FatPer100g)
{
    public FoodCommand ToCommand() => new(
        Name!, PortionName!, PortionGrams!.Value, EnergyKcalPer100g!.Value, ProteinPer100g!.Value, CarbsPer100g!.Value, FatPer100g!.Value);
}

/// <summary>
/// Dış kaynaktan aktarma: externalId ve source arama sonucundaki kimlik ve kaynak adı (source boşsa USDA);
/// ad ve porsiyon isteğe bağlı.
/// </summary>
public sealed record ImportFoodRequest(
    [Required, StringLength(50)] string? ExternalId,
    [StringLength(150)] string? Name,
    [StringLength(50)] string? PortionName,
    decimal? PortionGrams,
    [StringLength(50)] string? Source = null);

/// <summary>Oluşturulan kaydın kimliği.</summary>
public sealed record IdResponse(Guid Id);
