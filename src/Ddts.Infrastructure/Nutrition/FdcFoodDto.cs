namespace Ddts.Infrastructure.Nutrition;

/// <summary>USDA FoodData Central yanıtındaki besin (Adaptee'nin modeli, Şekil 4.9).</summary>
public sealed record FdcFoodDto(int FdcId, string Description, IReadOnlyList<FdcNutrientDto> FoodNutrients);

/// <summary>
/// Besin öğesi değeri (100 g için). Id: USDA nutrient id (1008 enerji, 1003 protein, 1004 yağ, 1005 karbonhidrat),
/// Number: eski numara ("208" enerji ...), UnitName: "KCAL", "G" ...
/// </summary>
public sealed record FdcNutrientDto(int? Id, string? Number, string? UnitName, decimal? Amount);
