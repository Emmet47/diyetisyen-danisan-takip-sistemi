using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Foods;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>
/// IFoodItemRepository'nin EF Core uygulaması. Diyetisyen ortak kayıtları (dietitian_id NULL)
/// ve kendi kayıtlarını görür; başka diyetisyenin kendi eklediği besin dönmez.
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

        return await db.FoodItems
            .AsNoTracking()
            .Where(f => ids.Contains(f.Id) && (f.DietitianId == null || f.DietitianId == dietitianId))
            .ToListAsync(cancellationToken);
    }

    public void Add(FoodItem food) => db.FoodItems.Add(food);
}
