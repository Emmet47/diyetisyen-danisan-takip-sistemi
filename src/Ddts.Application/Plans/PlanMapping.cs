using Ddts.Domain.Energy;
using Ddts.Domain.Foods;
using Ddts.Domain.Plans;

namespace Ddts.Application.Plans;

/// <summary>Plan, şablon ve öğünleri yanıt nesnelerine çevirir; toplamları Composite'ten (CalculateTotals) alır.</summary>
public static class PlanMapping
{
    public static NutrientTotalsDto ToDto(NutrientTotals totals) => new(
        Math.Round(totals.EnergyKcal, 1),
        Math.Round(totals.ProteinG, 1),
        Math.Round(totals.CarbsG, 1),
        Math.Round(totals.FatG, 1),
        totals.ProteinPercent,
        totals.CarbsPercent,
        totals.FatPercent);

    public static IReadOnlyList<MealDto> ToDtos(IEnumerable<Meal> meals) => meals
        .OrderBy(m => m.DayNo)
        .ThenBy(m => m.Time)
        .Select(ToDto)
        .ToList();

    public static MealDto ToDto(Meal meal) => new(
        meal.Id,
        meal.DayNo,
        meal.Name,
        meal.Time,
        meal.Items
            .OrderBy(i => i.IsAlternative)
            .Select(i => new MealItemDto(
                i.Id,
                i.FoodItemId,
                i.FoodItem.Name,
                i.FoodItem.PortionName,
                i.PortionCount,
                Math.Round(i.Grams(), 1),
                i.IsAlternative,
                i.AlternativeToItemId,
                ToDto(i.CalculateTotals())))
            .ToList(),
        ToDto(meal.CalculateTotals()));

    public static PlanSummaryDto ToSummary(NutritionPlan plan) => new(
        plan.Id,
        plan.ClientId,
        plan.Title,
        plan.PlanType,
        plan.Status,
        plan.TargetKcal,
        plan.EnergyFormula,
        plan.SourceTemplateId,
        plan.CreatedAt,
        plan.PublishedAt,
        plan.ArchivedAt,
        ToDtos(plan.Meals),
        plan.Meals.Select(m => m.DayNo).Distinct().Order()
            .Select(day => new DayTotalsDto(day, ToDto(plan.CalculateDayTotals(day))))
            .ToList(),
        ToDto(plan.CalculateTotals()),
        EnergyResult.DefaultDisclaimer);

    public static PlanListItemDto ToListItem(NutritionPlan plan) => new(
        plan.Id,
        plan.ClientId,
        plan.Title,
        plan.PlanType,
        plan.Status,
        plan.SourceTemplateId,
        plan.CreatedAt,
        plan.PublishedAt,
        plan.ArchivedAt);

    /// <summary>Girdideki bütün besin kimlikleri (ana satırlar ve alternatifler).</summary>
    public static IReadOnlyCollection<Guid> FoodIds(IEnumerable<MealInput> meals) => meals
        .SelectMany(m => m.Items ?? [])
        .SelectMany(i => (i.Alternatives ?? []).Select(a => a.FoodItemId).Prepend(i.FoodItemId))
        .ToHashSet();
}
