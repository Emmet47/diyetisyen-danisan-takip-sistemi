using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Application.Plans;
using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Energy;
using Ddts.Domain.Foods;
using Ddts.Domain.Plans;
using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Templates;

/// <summary>
/// Plan şablonları (F modülü). Prototype deseninin istemcisi (Şekil 4.8): kopyayı PlanTemplate.CreatePlanFor()
/// üretir, servis yalnızca kaydeder ve danışanın kısıtlarına göre uyarı listesi hazırlar (Şekil 5.3).
/// </summary>
public sealed class TemplateService(
    IPlanTemplateRepository templates,
    INutritionPlanRepository plans,
    IClientRepository clients,
    IFoodItemRepository foods,
    ISubscriptionRepository subscriptions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ITemplateService
{
    public async Task<Guid> CreateAsync(TemplateCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var meals = await BuildMealsAsync(dietitianId, command.Meals, cancellationToken);

        var template = new PlanTemplate(dietitianId, command.Name, command.Description, command.PlanType);
        template.Update(command.Name, command.Description, command.PlanType, meals);

        templates.Add(template);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return template.Id;
    }

    public async Task UpdateAsync(Guid templateId, TemplateCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var template = await GetOwnedTemplateAsync(dietitianId, templateId, cancellationToken);
        var meals = await BuildMealsAsync(dietitianId, command.Meals, cancellationToken);

        var oldMeals = template.Meals.ToList();
        template.Update(command.Name, command.Description, command.PlanType, meals);
        templates.RemoveMeals(oldMeals);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetOwnedTemplateAsync(DietitianId(), templateId, cancellationToken);

        // Bu şablondan kopyalanmış planlar kalır; yalnızca "kaynak şablon" bağlantıları boşalır.
        templates.Remove(template);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanCreatedDto> CopyToClientAsync(
        Guid templateId, Guid clientId, string? title, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var template = await GetOwnedTemplateAsync(dietitianId, templateId, cancellationToken);
        var client = await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        // BR-01: abonelik aktif değilse yeni plan oluşturulamaz.
        var subscription = await subscriptions.GetCurrentAsync(dietitianId, cancellationToken);
        if (subscription is null || !subscription.IsActiveOn(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)))
        {
            throw new SubscriptionLimitException("Aboneliğin aktif değil. Yeni plan hazırlamak için aboneliğini yenilemelisin.");
        }

        var plan = template.CreatePlanFor(client, string.IsNullOrWhiteSpace(title) ? template.Name : title);

        plans.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlanCreatedDto(
            plan.Id, plan.Status, plan.SourceTemplateId, PlanMapping.ToDto(plan.CalculateTotals()), RestrictionWarnings(plan, client));
    }

    public async Task<IReadOnlyList<TemplateListItemDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var list = await templates.ListAsync(DietitianId(), cancellationToken);
        return list.Select(t => new TemplateListItemDto(t.Id, t.Name, t.Description, t.PlanType, t.UpdatedAt)).ToList();
    }

    public async Task<TemplateSummaryDto> GetAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var template = await GetOwnedTemplateAsync(DietitianId(), templateId, cancellationToken);

        return new TemplateSummaryDto(
            template.Id,
            template.Name,
            template.Description,
            template.PlanType,
            template.UpdatedAt,
            PlanMapping.ToDtos(template.Meals),
            template.Meals.Select(m => m.DayNo).Distinct().Order()
                .Select(day => new DayTotalsDto(
                    day,
                    PlanMapping.ToDto(template.Meals.Where(m => m.DayNo == day)
                        .Aggregate(NutrientTotals.Zero, (sum, meal) => sum.Add(meal.CalculateTotals())))))
                .ToList(),
            PlanMapping.ToDto(template.CalculateTotals()),
            EnergyResult.DefaultDisclaimer);
    }

    /// <summary>Kopyadaki besinleri danışanın kısıtlarıyla karşılaştırır (FR-F2, örnek 4).</summary>
    private static IReadOnlyList<string> RestrictionWarnings(NutritionPlan plan, Client client) => plan.Meals
        .SelectMany(m => m.Items)
        .Select(i => client.GetRestriction(i.FoodItemId) switch
        {
            RestrictionType.Allergy => $"{i.FoodItem.Name}: danışanın alerji listesinde, yayımlamadan önce değiştirmelisin.",
            RestrictionType.Dislike => $"{i.FoodItem.Name}: danışan bu besini sevmiyor.",
            _ => null,
        })
        .OfType<string>()
        .Distinct()
        .ToList();

    /// <summary>
    /// Şablon öğünlerini kurar. Şablon danışana bağlı olmadığı için kısıt denetimi yok (kopyalanırken yapılır);
    /// hatalar toplanıp birlikte bildirilir.
    /// </summary>
    private async Task<IReadOnlyList<Meal>> BuildMealsAsync(
        Guid dietitianId, IReadOnlyList<MealInput>? inputs, CancellationToken cancellationToken)
    {
        var mealInputs = inputs ?? [];
        var ids = PlanMapping.FoodIds(mealInputs);
        var foodMap = (await foods.GetManyAsync(dietitianId, ids, cancellationToken)).ToDictionary(f => f.Id);
        var errors = ids.Where(id => !foodMap.ContainsKey(id)).Select(id => $"Besin bulunamadı: {id}.").ToList();

        if (mealInputs.Count == 0)
        {
            errors.Add("Şablona en az bir öğün eklenmeli.");
        }

        var meals = new List<Meal>();
        foreach (var input in mealInputs)
        {
            var label = $"{input.DayNo}. gün {input.Name}".TrimEnd();
            try
            {
                var meal = new Meal(input.DayNo, input.Name, input.Time);

                foreach (var item in input.Items ?? [])
                {
                    if (!foodMap.TryGetValue(item.FoodItemId, out var food))
                    {
                        continue;
                    }

                    var main = new MealItem(food, item.PortionCount);
                    meal.AddItem(main);

                    foreach (var alternative in item.Alternatives ?? [])
                    {
                        if (foodMap.TryGetValue(alternative.FoodItemId, out var alternativeFood))
                        {
                            meal.AddItem(new MealItem(alternativeFood, alternative.PortionCount, main.Id));
                        }
                    }
                }

                if (!meal.Items.Any(i => !i.IsAlternative))
                {
                    errors.Add($"{label}: öğünde en az bir besin olmalı.");
                }

                meals.Add(meal);
            }
            catch (DomainException e)
            {
                errors.Add($"{label}: {e.Message}");
            }
        }

        if (errors.Count > 0)
        {
            throw new DomainException(errors.Count == 1 ? errors[0] : $"Şablon kaydedilemedi: {errors.Count} hata var.", errors);
        }

        return meals;
    }

    private async Task<PlanTemplate> GetOwnedTemplateAsync(Guid dietitianId, Guid templateId, CancellationToken cancellationToken) =>
        await templates.GetAsync(dietitianId, templateId, cancellationToken)
        ?? throw new NotFoundException("Şablon bulunamadı.");

    private Guid DietitianId() =>
        currentUser.DietitianId ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");
}
