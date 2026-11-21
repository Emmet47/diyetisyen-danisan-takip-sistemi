using Ddts.Application.Abstractions.Nutrition;
using Ddts.Application.Abstractions.Security;
using Ddts.Domain.Foods;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// Yerel veritabanı adapter'ı (Şekil 4.9). Diyetisyenin göreceği kayıtlar: ortak besinler + kendi kayıtları
/// (PlateRank ve USDA'dan aktardıkları dahil). Ad üzerinde büyük/küçük harf duyarsız arama (PostgreSQL ILIKE).
/// Bu kayıtları bir diyetisyen girdiği ya da kontrol edip kaydettiği için "doğrulanmış kayıt" sayıyoruz (FR-D4).
/// </summary>
public sealed class LocalNutritionDataAdapter(DdtsDbContext db, ICurrentUser currentUser) : NutritionDataPort
{
    public string SourceName => "Yerel";

    public string LanguageCode => "tr";

    public async Task<IReadOnlyList<NutritionItem>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var foods = Visible();

        if (!string.IsNullOrWhiteSpace(query))
        {
            // % ve _ kullanıcı girdisinde joker karakter sayılmasın diye kaçışlanır.
            var pattern = "%" + query.Trim().Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_") + "%";
            foods = foods.Where(f => EF.Functions.ILike(f.Name, pattern, @"\"));
        }

        var list = await foods.OrderBy(f => f.Name).ThenBy(f => f.Id).Take(limit).ToListAsync(cancellationToken);
        return list.Select(ToItem).ToList();
    }

    public async Task<NutritionItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        var food = await Visible().FirstOrDefaultAsync(f => f.ExternalId == externalId, cancellationToken);
        return food is null ? null : ToItem(food);
    }

    private IQueryable<FoodItem> Visible()
    {
        var dietitianId = currentUser.DietitianId;
        return db.FoodItems.AsNoTracking().Where(f => f.DietitianId == null || f.DietitianId == dietitianId);
    }

    private static NutritionItem ToItem(FoodItem food) => new(
        food.Id,
        food.Name,
        food.PortionName,
        food.PortionGrams,
        food.EnergyKcalPer100g,
        food.ProteinPer100g,
        food.CarbsPer100g,
        food.FatPer100g,
        food.SourceName,
        food.ExternalId,
        food.SourceUpdatedAt);
}
