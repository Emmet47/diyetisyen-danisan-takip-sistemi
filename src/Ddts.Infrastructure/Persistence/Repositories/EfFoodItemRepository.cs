using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Foods;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>
/// IFoodItemRepository'nin EF Core uygulaması. Diyetisyen ortak kayıtları (dietitian_id boş) ve kendi kayıtlarını görür.
/// Okunan besinler izlenir (tracked): plan satırları bu nesnelere bağlandığında EF besini yeniden eklemeye çalışmaz.
/// </summary>
public sealed class EfFoodItemRepository(DdtsDbContext db) : IFoodItemRepository
{
    public async Task<IReadOnlyList<FoodItem>> GetManyAsync(
        Guid dietitianId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await Visible(dietitianId)
            .Where(f => ids.Contains(f.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<FoodItem?> GetAsync(Guid dietitianId, Guid foodItemId, CancellationToken cancellationToken = default) =>
        Visible(dietitianId).SingleOrDefaultAsync(f => f.Id == foodItemId, cancellationToken);

    public Task<FoodItem?> GetByExternalIdAsync(
        Guid dietitianId, string sourceName, string externalId, CancellationToken cancellationToken = default) =>
        db.FoodItems.FirstOrDefaultAsync(
            f => f.DietitianId == dietitianId && f.SourceName == sourceName && f.ExternalId == externalId,
            cancellationToken);

    public Task<bool> IsInUseAsync(Guid foodItemId, CancellationToken cancellationToken = default) =>
        db.Set<Ddts.Domain.Plans.MealItem>().AnyAsync(i => i.FoodItemId == foodItemId, cancellationToken);

    public void Add(FoodItem food) => db.FoodItems.Add(food);

    public void Remove(FoodItem food) => db.FoodItems.Remove(food);

    private IQueryable<FoodItem> Visible(Guid dietitianId) =>
        db.FoodItems.Where(f => f.DietitianId == null || f.DietitianId == dietitianId);
}
