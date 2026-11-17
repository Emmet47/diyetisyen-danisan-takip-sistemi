namespace Ddts.Application.Abstractions.Nutrition;

/// <summary>
/// Sistemin standart besin modeli (Şekil 4.9). Adapter'lar kendi kaynaklarının yanıtını buna çevirir.
/// Değerler 100 g içindir. Id yalnızca yerel kayıtta dolu (plana eklenecek besinin kimliği);
/// ExternalId dış kaynaktaki kimlik (USDA fdcId). RetrievedAt verinin kaynaktan alındığı/güncellendiği zaman.
/// </summary>
public sealed record NutritionItem(
    Guid? Id,
    string Name,
    string PortionName,
    decimal PortionGrams,
    decimal EnergyKcalPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    string SourceName,
    string? ExternalId,
    DateTime? RetrievedAt);
