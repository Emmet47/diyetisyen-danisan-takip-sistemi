using Ddts.Application.Common;
using Ddts.Domain.Plans;

namespace Ddts.Application.MealLogs;

/// <summary>
/// Bir günün planlı öğünlerini bulur. O gün geçerli plan: o gün ya da önce yayımlanmış, o günden sonra
/// arşivlenmiş (ya da hâlâ yayında) plan. Günlük planda her gün 1. günün öğünleri, haftalık planda
/// gün numarası haftanın günüdür (1 = Pazartesi … 7 = Pazar). Karşılaştırma (FR-G3) ve rapor (FR-K3) bunu kullanır.
/// </summary>
public static class PlannedMeals
{
    public static int DayNoFor(PlanType planType, DateOnly date) =>
        planType == PlanType.Daily ? 1 : LocalTime.Weekday(date);

    public static IReadOnlyList<Meal> For(NutritionPlan plan, DateOnly date) => plan.Meals
        .Where(m => m.DayNo == DayNoFor(plan.PlanType, date))
        .OrderBy(m => m.Time)
        .ToList();

    public static NutritionPlan? ActivePlanOn(IEnumerable<NutritionPlan> plans, DateOnly date) => plans
        .Where(p => p.PublishedAt is { } published && LocalTime.DateOf(published) <= date)
        .Where(p => p.ArchivedAt is not { } archived || LocalTime.DateOf(archived) > date)
        .OrderByDescending(p => p.PublishedAt)
        .FirstOrDefault();

    /// <summary>Aralıktaki her gün ve o günün planlı öğünleri.</summary>
    public static IEnumerable<(DateOnly Date, IReadOnlyList<Meal> Meals)> Between(
        IReadOnlyList<NutritionPlan> plans, DateOnly from, DateOnly to)
    {
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var plan = ActivePlanOn(plans, date);
            yield return (date, plan is null ? [] : For(plan, date));
        }
    }
}
