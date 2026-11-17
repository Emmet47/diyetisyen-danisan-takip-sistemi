using Ddts.Domain.Plans;

namespace Ddts.Application.Plans;

/// <summary>Besin satırının alternatifi (FR-E2): "2 yumurta yerine 2 dilim peynir". Toplama katılmaz.</summary>
public sealed record AlternativeInput(Guid FoodItemId, decimal PortionCount);

/// <summary>Öğündeki ana besin satırı ve (varsa) alternatifleri.</summary>
public sealed record MealItemInput(Guid FoodItemId, decimal PortionCount, IReadOnlyList<AlternativeInput>? Alternatives);

/// <summary>Öğün: gün (günlük planda 1, haftalıkta 1 = Pazartesi … 7 = Pazar), ad, saat ve besinler.</summary>
public sealed record MealInput(int DayNo, string Name, TimeOnly Time, IReadOnlyList<MealItemInput> Items);

/// <summary>Plan sihirbazının girdisi (FR-E1, Şekil 5.2).</summary>
public sealed record CreatePlanCommand(
    Guid ClientId,
    string Title,
    PlanType PlanType,
    int? TargetKcal,
    string? EnergyFormula,
    IReadOnlyList<MealInput> Meals);

/// <summary>Taslak planı düzenleme: içerik baştan verilir (danışan değişmez).</summary>
public sealed record UpdatePlanCommand(
    string Title,
    PlanType PlanType,
    int? TargetKcal,
    string? EnergyFormula,
    IReadOnlyList<MealInput> Meals);

/// <summary>Enerji ve makro toplamları (FR-H2). Değerler 1 basamağa yuvarlanır; yüzdeler makro enerjisine göre.</summary>
public sealed record NutrientTotalsDto(
    decimal EnergyKcal,
    decimal ProteinG,
    decimal CarbsG,
    decimal FatG,
    decimal ProteinPercent,
    decimal CarbsPercent,
    decimal FatPercent);

public sealed record MealItemDto(
    Guid Id,
    Guid FoodItemId,
    string FoodName,
    string PortionName,
    decimal PortionCount,
    decimal Grams,
    bool IsAlternative,
    Guid? AlternativeToItemId,
    NutrientTotalsDto Totals);

public sealed record MealDto(Guid Id, int DayNo, string Name, TimeOnly Time, IReadOnlyList<MealItemDto> Items, NutrientTotalsDto Totals);

public sealed record DayTotalsDto(int DayNo, NutrientTotalsDto Totals);

/// <summary>
/// Planın tamamı ve Composite toplamları (Şekil 4.5): satır → öğün → gün → plan. Disclaimer: FR-H3.
/// </summary>
public sealed record PlanSummaryDto(
    Guid Id,
    Guid ClientId,
    string Title,
    PlanType PlanType,
    PlanStatus Status,
    int? TargetKcal,
    string? EnergyFormula,
    Guid? SourceTemplateId,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? ArchivedAt,
    IReadOnlyList<MealDto> Meals,
    IReadOnlyList<DayTotalsDto> Days,
    NutrientTotalsDto Totals,
    string Disclaimer);

/// <summary>Plan listesi satırı.</summary>
public sealed record PlanListItemDto(
    Guid Id,
    Guid ClientId,
    string Title,
    PlanType PlanType,
    PlanStatus Status,
    Guid? SourceTemplateId,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? ArchivedAt);

/// <summary>
/// Yeni (ya da düzenlenen) taslak planın sonucu: kimlik, toplamlar ve uyarılar (ör. danışanın sevmediği besin,
/// şablon kopyasında alerji listesindeki besin). Şablondan geldiyse SourceTemplateId dolu (Şekil 5.3).
/// </summary>
public sealed record PlanCreatedDto(
    Guid PlanId,
    PlanStatus Status,
    Guid? SourceTemplateId,
    NutrientTotalsDto Totals,
    IReadOnlyList<string> Warnings);
