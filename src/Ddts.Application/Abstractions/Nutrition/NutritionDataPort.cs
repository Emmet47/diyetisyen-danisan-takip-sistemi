namespace Ddts.Application.Abstractions.Nutrition;

/// <summary>
/// Adapter deseninin hedef arayüzü (Target / port, Şekil 4.9). Her besin kaynağı (yerel veritabanı, PlateRank,
/// USDA ...) bu arayüzü uygulayan bir adapter'dır; FoodSearchService hangi kaynağı kullandığını bilmez.
/// Yeni kaynak = yeni adapter + Program.cs'te kayıt (NFR-B1). Adı proje tanımındaki gibi NutritionDataPort.
/// </summary>
public interface NutritionDataPort
{
    /// <summary>Kaynağın adı; sonuçlarda "veri kaynağı" olarak görünür (ör. "Yerel", "PlateRank", "USDA FoodData Central").</summary>
    string SourceName { get; }

    /// <summary>
    /// Kaynaktaki besin adlarının dili (ISO 639-1: "tr", "en"). Türkçe değilse FoodSearchService sorguyu bu dile,
    /// gelen adları da Türkçeye çevirir (ITranslator).
    /// </summary>
    string LanguageCode { get; }

    Task<IReadOnlyList<NutritionItem>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);

    Task<NutritionItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);
}
