using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>
/// INutritionPlanRepository'nin EF Core uygulaması. Tek plan okunurken öğünler, satırlar ve besinler birlikte
/// yüklenir (Include): Composite toplamları (CalculateTotals) bu nesnelerle hesaplanır.
/// </summary>
public sealed class EfNutritionPlanRepository(DdtsDbContext db) : INutritionPlanRepository
{
    public Task<NutritionPlan?> GetAsync(Guid dietitianId, Guid planId, CancellationToken cancellationToken = default) =>
        WithContent().SingleOrDefaultAsync(p => p.Id == planId && p.DietitianId == dietitianId, cancellationToken);

    public Task<NutritionPlan?> GetPublishedForClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        WithContent().SingleOrDefaultAsync(p => p.ClientId == clientId && p.Status == PlanStatus.Published, cancellationToken);

    public async Task<IReadOnlyList<NutritionPlan>> ListAsync(
        Guid dietitianId, Guid? clientId, CancellationToken cancellationToken = default)
    {
        var query = db.NutritionPlans.AsNoTracking().Where(p => p.DietitianId == dietitianId);

        if (clientId is { } id)
        {
            query = query.Where(p => p.ClientId == id);
        }

        return await query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NutritionPlan>> ListForClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        await db.NutritionPlans
            .AsNoTracking()
            .Include(p => p.Meals)
            .Where(p => p.ClientId == clientId && p.Status != PlanStatus.Draft)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NutritionPlan>> ListPublishedAsync(CancellationToken cancellationToken = default) =>
        await db.NutritionPlans
            .AsNoTracking()
            .Include(p => p.Meals)
            .Where(p => p.Status == PlanStatus.Published)
            .ToListAsync(cancellationToken);

    public void Add(NutritionPlan plan) => db.NutritionPlans.Add(plan);

    public void Remove(NutritionPlan plan) => db.NutritionPlans.Remove(plan);

    public void RemoveMeals(IEnumerable<Meal> meals) => db.Set<Meal>().RemoveRange(meals);

    private IQueryable<NutritionPlan> WithContent() => db.NutritionPlans
        .Include(p => p.Meals)
        .ThenInclude(m => m.Items)
        .ThenInclude(i => i.FoodItem)
        .AsSplitQuery();
}
