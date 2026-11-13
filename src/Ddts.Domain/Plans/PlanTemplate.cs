using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Foods;

namespace Ddts.Domain.Plans;

/// <summary>
/// Tekrar kullanılabilir plan şablonu (Şekil 4.2, FR-F1). Composite kökü (Şekil 4.5);
/// CreatePlanFor ile öğünlerinin derin kopyasından danışana taslak plan üretir (Prototype, Şekil 4.8).
/// </summary>
public sealed class PlanTemplate : INutritionComponent
{
    private readonly List<Meal> _meals = [];

    private PlanTemplate()
    {
    }

    public PlanTemplate(Guid dietitianId, string name, string? description, PlanType planType)
    {
        Id = Guid.CreateVersion7();
        DietitianId = dietitianId;
        Name = Guard.Required(name, 100, "Şablon adı");
        Description = Guard.Optional(description, 500, "Açıklama");
        PlanType = planType;
        UpdatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>Şablonun sahibi olan diyetisyen.</summary>
    public Guid DietitianId { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public PlanType PlanType { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();

    public void AddMeal(Meal meal)
    {
        if (PlanType == PlanType.Daily && meal.DayNo != 1)
        {
            throw new DomainException("Günlük şablonda öğünler 1. güne eklenir.");
        }

        _meals.Add(meal);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Danışana bu şablondan bağımsız bir taslak plan oluşturur: her öğün Clone() ile yeni kimlikli
    /// kopya olur. Kopyada yapılan değişiklik şablonu etkilemez (BR-07, Şekil 5.3).
    /// </summary>
    public NutritionPlan CreatePlanFor(Client client, string title)
    {
        if (client.DietitianId != DietitianId)
        {
            throw new DomainException("Şablon yalnızca kendi danışanınıza kopyalanabilir.");
        }

        var plan = NutritionPlan.CreateDraft(client, title, PlanType, Id);

        foreach (var meal in _meals)
        {
            plan.AddMeal(meal.Clone());
        }

        return plan;
    }

    public NutrientTotals CalculateTotals() =>
        _meals.Aggregate(NutrientTotals.Zero, (sum, meal) => sum.Add(meal.CalculateTotals()));
}
