using System.Text.Json.Serialization;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// PlateRank "yemekler" tablosundaki bir satır (Adaptee'nin modeli, Şekil 4.9). Yalnızca kullandığımız sütunlar.
/// Değerler 1 porsiyon içindir; PortionGrams (porsiyon_g) çoğu satırda boştur (Türk yemeklerinin hepsinde).
/// Source (kaynak) boşsa değerler PlateRank'in kendi tahminidir (CC BY 4.0); doluysa verinin alındığı yer yazar.
/// </summary>
public sealed record PlateRankDishDto(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("ad")] string? Name,
    [property: JsonPropertyName("mutfak")] string? Cuisine,
    [property: JsonPropertyName("kcal")] decimal? EnergyKcal,
    [property: JsonPropertyName("protein")] decimal? Protein,
    [property: JsonPropertyName("karbonhidrat")] decimal? Carbs,
    [property: JsonPropertyName("yag")] decimal? Fat,
    [property: JsonPropertyName("porsiyon_g")] decimal? PortionGrams,
    [property: JsonPropertyName("kaynak")] string? Source,
    [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt);
