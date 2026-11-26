namespace Ddts.Application.Foods;

/// <summary>Gelen port: besin arama ve dış kaynaktan içe aktarma (FR-D1, FR-D3, FR-D4, Şekil 4.9 ve 5.8).</summary>
public interface IFoodSearchService
{
    /// <summary>
    /// Yerel kayıtlarda arar; includeExternal ise dış kaynaklarda (PlateRank, USDA) da arar. source "Yerel",
    /// "PlateRank" ya da "USDA" ise yalnızca o kaynaktan gelen kayıtlar döner.
    /// </summary>
    Task<FoodSearchResult> SearchAsync(string? query, string? source, bool includeExternal, CancellationToken cancellationToken = default);

    /// <summary>Dış kaynaktaki besini yerel veritabanına aktarır; daha önce aktarıldıysa mevcut kaydın kimliği döner.</summary>
    Task<Guid> ImportAsync(ImportFoodCommand command, CancellationToken cancellationToken = default);
}
