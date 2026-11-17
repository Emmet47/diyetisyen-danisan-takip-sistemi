using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Foods;

namespace Ddts.Domain.Plans;

/// <summary>
/// Builder deseni (Şekil 4.6, FR-E1, BR-05): beslenme planını danışanın hedef ve kısıtlarına göre adım adım kurar.
/// Adımlar hata fırlatmaz, hataları toplar; Build() hepsini birlikte bildirir (sihirbazda kullanıcı tüm
/// eksikleri tek seferde görür). Alerji kısıtlı besin hata, "sevmiyor" kısıtlı besin uyarıdır.
/// </summary>
public sealed class NutritionPlanBuilder
{
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];
    private readonly List<Meal> _meals = [];

    private Client? _client;
    private string? _title;
    private PlanType _type = PlanType.Weekly;
    private int? _targetKcal;
    private string? _energyFormula;
    private Meal? _currentMeal;
    private MealItem? _lastMainItem;

    public NutritionPlanBuilder ForClient(Client client)
    {
        _client = client;
        return this;
    }

    public NutritionPlanBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public NutritionPlanBuilder WithType(PlanType type)
    {
        _type = type;
        return this;
    }

    public NutritionPlanBuilder WithEnergyTarget(int kcal, string formula)
    {
        _targetKcal = kcal;
        _energyFormula = formula;
        return this;
    }

    /// <summary>Yeni öğün başlatır; sonraki AddFood/AddAlternative çağrıları bu öğüne eklenir.</summary>
    public NutritionPlanBuilder AddMeal(int dayNo, string name, TimeOnly time)
    {
        _lastMainItem = null;

        try
        {
            _currentMeal = new Meal(dayNo, name, time);
            _meals.Add(_currentMeal);
        }
        catch (DomainException e)
        {
            _currentMeal = null;
            _errors.Add($"{MealLabel(dayNo, name)}: {e.Message}");
        }

        return this;
    }

    /// <summary>Son eklenen öğüne ana besin satırı ekler.</summary>
    public NutritionPlanBuilder AddFood(FoodItem food, decimal portions)
    {
        if (_currentMeal is null)
        {
            _errors.Add($"{food.Name}: önce geçerli bir öğün eklenmeli.");
            return this;
        }

        if (!PassesRestrictions(food))
        {
            _lastMainItem = null;
            return this;
        }

        try
        {
            var item = new MealItem(food, portions);
            _currentMeal.AddItem(item);
            _lastMainItem = item;
        }
        catch (DomainException e)
        {
            _lastMainItem = null;
            _errors.Add($"{_currentMeal.Name} – {food.Name}: {e.Message}");
        }

        return this;
    }

    /// <summary>Son eklenen ana satıra alternatif ekler (FR-E2): "2 yumurta yerine 2 dilim peynir".</summary>
    public NutritionPlanBuilder AddAlternative(FoodItem food, decimal portions)
    {
        if (_currentMeal is null || _lastMainItem is null)
        {
            _errors.Add($"{food.Name}: alternatif, aynı öğündeki bir ana besinden sonra eklenmeli.");
            return this;
        }

        if (!PassesRestrictions(food))
        {
            return this;
        }

        try
        {
            _currentMeal.AddItem(new MealItem(food, portions, _lastMainItem.Id));
        }
        catch (DomainException e)
        {
            _errors.Add($"{_currentMeal.Name} – {food.Name}: {e.Message}");
        }

        return this;
    }

    public IReadOnlyList<string> Warnings() => _warnings.AsReadOnly();

    /// <summary>
    /// Planı Taslak (Draft) olarak üretir. Zorunlu: danışan, başlık, en az bir öğün, her öğünde en az bir
    /// ana besin; günlük planda öğünler 1. günde. Hata varsa hepsini içeren DomainException fırlatır.
    /// </summary>
    public NutritionPlan Build()
    {
        var errors = new List<string>(_errors);

        if (_client is null)
        {
            errors.Add("Planın danışanı seçilmeli.");
        }

        if (string.IsNullOrWhiteSpace(_title))
        {
            errors.Add("Plan başlığı boş olamaz.");
        }

        if (_targetKcal is < 800 or > 6000)
        {
            errors.Add("Hedef enerji 800 ile 6000 kcal arasında olmalı.");
        }

        if (_meals.Count == 0)
        {
            errors.Add("Plana en az bir öğün eklenmeli.");
        }

        foreach (var meal in _meals)
        {
            if (!meal.Items.Any(i => !i.IsAlternative))
            {
                errors.Add($"{MealLabel(meal.DayNo, meal.Name)}: öğünde en az bir besin olmalı.");
            }

            if (_type == PlanType.Daily && meal.DayNo != 1)
            {
                errors.Add($"{MealLabel(meal.DayNo, meal.Name)}: günlük planda öğünler 1. güne eklenir.");
            }
        }

        if (errors.Count > 0)
        {
            throw new DomainException(
                errors.Count == 1 ? errors[0] : $"Plan kaydedilemedi: {errors.Count} hata var.",
                errors);
        }

        var plan = NutritionPlan.CreateDraft(_client!, _title!, _type, sourceTemplateId: null);

        if (_targetKcal is { } kcal)
        {
            plan.SetEnergyTarget(kcal, _energyFormula ?? "Elle");
        }

        foreach (var meal in _meals)
        {
            plan.AddMeal(meal);
        }

        return plan;
    }

    // BR-05: alerji listesindeki besin plana giremez (hata), "sevmiyor" listesindeki besin uyarı verir.
    private bool PassesRestrictions(FoodItem food)
    {
        switch (_client?.GetRestriction(food.Id))
        {
            case RestrictionType.Allergy:
                _errors.Add($"{food.Name}: danışanın alerji listesinde, plana eklenemez.");
                return false;

            case RestrictionType.Dislike:
                var warning = $"{food.Name}: danışan bu besini sevmiyor.";
                if (!_warnings.Contains(warning))
                {
                    _warnings.Add(warning);
                }

                return true;

            default:
                return true;
        }
    }

    private static string MealLabel(int dayNo, string? name) => $"{dayNo}. gün {name}".TrimEnd();
}
