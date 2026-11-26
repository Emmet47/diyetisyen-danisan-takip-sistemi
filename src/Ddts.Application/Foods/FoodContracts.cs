using Ddts.Application.Abstractions.Nutrition;

namespace Ddts.Application.Foods;

/// <summary>Yerel besin ekleme/düzenleme girdisi (FR-D2). Enerji ve makrolar 100 g içindir.</summary>
public sealed record FoodCommand(
    string Name,
    string PortionName,
    decimal PortionGrams,
    decimal EnergyKcalPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g);

/// <summary>
/// Dış kaynaktaki sonucu yerel veritabanına aktarma (FR-D3). Diyetisyen değerleri kontrol edip Türkçe ad ve
/// porsiyon verebilir; boş bırakılanlar kaynaktaki ad ve porsiyon olur. Besin değerleri kaynaktan alınır.
/// Source: sonucun geldiği kaynak ("PlateRank", "USDA"); boşsa USDA sayılır (eski istemciler için).
/// Kaynak porsiyon gramını vermiyorsa (ValuesPerPortion) PortionGrams zorunludur.
/// </summary>
public sealed record ImportFoodCommand(
    string ExternalId,
    string? Name,
    string? PortionName,
    decimal? PortionGrams,
    string? Source = null);

/// <summary>
/// Besin arama sonucu (Şekil 4.9, FR-D1, FR-D4). Dış kaynaklardan biri kullanılamadıysa ExternalUnavailable = true
/// ve Message dolu olur; yerel sonuçlar ve çalışan kaynağın sonuçları yine döner.
/// </summary>
public sealed record FoodSearchResult(IReadOnlyList<NutritionItem> Items, bool ExternalUnavailable, string? Message);
