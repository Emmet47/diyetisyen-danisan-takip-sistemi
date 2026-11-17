using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Foods;
using Ddts.Domain.Plans;
using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Plans;

/// <summary>
/// Beslenme planı use case'leri (E modülü). Builder deseninde Director (Şekil 4.6): adımları sırayla çağırır,
/// Composite deseninde istemci (Şekil 4.5): toplamları plandan ister. Yayımlama Şekil 5.4'teki gibi
/// tek transaction'da eski planı arşivler, yenisini yayımlar; bildirim commit'ten sonra domain event ile gider.
/// </summary>
public sealed class PlanService(
    INutritionPlanRepository plans,
    IClientRepository clients,
    IFoodItemRepository foods,
    ISubscriptionRepository subscriptions,
    IUnitOfWork unitOfWork,
    IAuditLogger auditLogger,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IPlanService
{
    public const string AuditEntityType = "NutritionPlan";

    public async Task<PlanCreatedDto> CreatePlanAsync(CreatePlanCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var client = await clients.GetAsync(dietitianId, command.ClientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        await EnsureSubscriptionActiveAsync(dietitianId, cancellationToken);

        var (plan, warnings) = await BuildAsync(
            client, command.Title, command.PlanType, command.TargetKcal, command.EnergyFormula, command.Meals, cancellationToken);

        plans.Add(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlanCreatedDto(plan.Id, plan.Status, plan.SourceTemplateId, PlanMapping.ToDto(plan.CalculateTotals()), warnings);
    }

    public async Task<PlanCreatedDto> UpdateDraftAsync(Guid planId, UpdatePlanCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var plan = await GetOwnedPlanAsync(dietitianId, planId, cancellationToken);
        EnsureDraft(plan, "Yalnızca taslak plan düzenlenebilir. Değişiklik için yeni plan hazırla.");

        var client = await clients.GetAsync(dietitianId, plan.ClientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        var (draft, warnings) = await BuildAsync(
            client, command.Title, command.PlanType, command.TargetKcal, command.EnergyFormula, command.Meals, cancellationToken);

        var oldMeals = plan.Meals.ToList();
        plan.UpdateDraft(draft);
        plans.RemoveMeals(oldMeals);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlanCreatedDto(plan.Id, plan.Status, plan.SourceTemplateId, PlanMapping.ToDto(plan.CalculateTotals()), warnings);
    }

    public async Task PublishAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();
        var plan = await GetOwnedPlanAsync(dietitianId, planId, cancellationToken);
        EnsureDraft(plan, "Yalnızca taslak plan yayımlanabilir.");

        var client = await clients.GetAsync(dietitianId, plan.ClientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        // BR-05: danışanın alerji listesindeki besin (alternatif satırlar dahil) planda kalamaz.
        var allergens = plan.Meals
            .SelectMany(m => m.Items)
            .Where(i => client.GetRestriction(i.FoodItemId) == RestrictionType.Allergy)
            .Select(i => i.FoodItem.Name)
            .Distinct()
            .ToList();

        if (allergens.Count > 0)
        {
            throw new DomainException($"Alerjen besin içeren plan yayımlanamaz: {string.Join(", ", allergens)}.");
        }

        var previous = await plans.GetPublishedForClientAsync(client.Id, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Eski planın arşivi ve yeni planın yayımı tek transaction: danışanın hiçbir an iki yayımlanmış planı
        // olmaz (kısmi tekil indeks ux_nutrition_plans_one_published). PlanPublished olayı commit'ten sonra dağıtılır.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (previous is not null && previous.Id != plan.Id)
            {
                previous.Archive(now);
                await auditLogger.LogAsync("PlanArchived", AuditEntityType, previous.Id, null, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            plan.Publish(now);
            await auditLogger.LogAsync("PlanPublished", AuditEntityType, plan.Id, null, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task ArchiveAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetOwnedPlanAsync(DietitianId(), planId, cancellationToken);

        if (plan.Status != PlanStatus.Published)
        {
            throw new ConflictException("Yalnızca yayımlanmış plan arşivlenebilir.");
        }

        plan.Archive(timeProvider.GetUtcNow().UtcDateTime);
        await auditLogger.LogAsync("PlanArchived", AuditEntityType, plan.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanSummaryDto> GetPlanSummaryAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetOwnedPlanAsync(DietitianId(), planId, cancellationToken);
        return PlanMapping.ToSummary(plan);
    }

    public async Task<IReadOnlyList<PlanListItemDto>> ListAsync(Guid? clientId, CancellationToken cancellationToken = default)
    {
        var dietitianId = DietitianId();

        if (clientId is { } id && await clients.GetAsync(dietitianId, id, cancellationToken) is null)
        {
            throw new NotFoundException("Danışan bulunamadı.");
        }

        var list = await plans.ListAsync(dietitianId, clientId, cancellationToken);
        return list.Select(PlanMapping.ToListItem).ToList();
    }

    public async Task DeleteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var plan = await GetOwnedPlanAsync(DietitianId(), planId, cancellationToken);
        EnsureDraft(plan, "Yalnızca taslak plan silinebilir.");

        plans.Remove(plan);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PlanSummaryDto> GetMyPlanAsync(CancellationToken cancellationToken = default)
    {
        var clientId = currentUser.ClientId
            ?? throw new InvalidOperationException("Bu işlem yalnızca danışan hesabıyla yapılabilir.");

        var plan = await plans.GetPublishedForClientAsync(clientId, cancellationToken)
            ?? throw new NotFoundException("Henüz yayımlanmış bir planın yok.");

        return PlanMapping.ToSummary(plan);
    }

    /// <summary>Builder'ın adımlarını girdideki sırayla çağırır (Director rolü, Şekil 5.2).</summary>
    private async Task<(NutritionPlan Plan, IReadOnlyList<string> Warnings)> BuildAsync(
        Client client,
        string title,
        PlanType planType,
        int? targetKcal,
        string? energyFormula,
        IReadOnlyList<MealInput>? meals,
        CancellationToken cancellationToken)
    {
        var mealInputs = meals ?? [];
        var foodMap = await LoadFoodsAsync(client.DietitianId, mealInputs, cancellationToken);

        var builder = new NutritionPlanBuilder()
            .ForClient(client)
            .WithTitle(title)
            .WithType(planType);

        if (targetKcal is { } kcal)
        {
            builder.WithEnergyTarget(kcal, string.IsNullOrWhiteSpace(energyFormula) ? "Elle" : energyFormula);
        }

        foreach (var meal in mealInputs)
        {
            builder.AddMeal(meal.DayNo, meal.Name, meal.Time);

            foreach (var item in meal.Items ?? [])
            {
                builder.AddFood(foodMap[item.FoodItemId], item.PortionCount);

                foreach (var alternative in item.Alternatives ?? [])
                {
                    builder.AddAlternative(foodMap[alternative.FoodItemId], alternative.PortionCount);
                }
            }
        }

        var plan = builder.Build();
        return (plan, builder.Warnings());
    }

    /// <summary>Girdideki besinleri getirir; diyetisyenin göremediği (başkasının) besin "bulunamadı" sayılır.</summary>
    private async Task<IReadOnlyDictionary<Guid, FoodItem>> LoadFoodsAsync(
        Guid dietitianId, IReadOnlyList<MealInput> meals, CancellationToken cancellationToken)
    {
        var ids = PlanMapping.FoodIds(meals);
        var found = await foods.GetManyAsync(dietitianId, ids, cancellationToken);
        var map = found.ToDictionary(f => f.Id);

        var missing = ids.Where(id => !map.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new DomainException($"Besin bulunamadı: {string.Join(", ", missing)}.");
        }

        return map;
    }

    private async Task EnsureSubscriptionActiveAsync(Guid dietitianId, CancellationToken cancellationToken)
    {
        // BR-01: abonelik aktif değilse yeni plan hazırlanamaz.
        var subscription = await subscriptions.GetCurrentAsync(dietitianId, cancellationToken);
        if (subscription is null || !subscription.IsActiveOn(DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)))
        {
            throw new SubscriptionLimitException("Aboneliğin aktif değil. Yeni plan hazırlamak için aboneliğini yenilemelisin.");
        }
    }

    private async Task<NutritionPlan> GetOwnedPlanAsync(Guid dietitianId, Guid planId, CancellationToken cancellationToken) =>
        await plans.GetAsync(dietitianId, planId, cancellationToken)
        ?? throw new NotFoundException("Plan bulunamadı.");

    private static void EnsureDraft(NutritionPlan plan, string message)
    {
        if (plan.Status != PlanStatus.Draft)
        {
            throw new ConflictException(message);
        }
    }

    private Guid DietitianId() =>
        currentUser.DietitianId ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");
}
