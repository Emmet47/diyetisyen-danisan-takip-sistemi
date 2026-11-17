using Ddts.Domain.Foods;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Besin kayıtları (Şekil 4.9, 4.10). Diyetisyen ortak kayıtları (DietitianId boş) ve kendi kayıtlarını görür;
/// başka diyetisyenin kaydı hiç dönmez. Yerel arama LocalNutritionDataAdapter'da yapılır.
/// </summary>
public interface IFoodItemRepository
{
    /// <summary>Verilen kimliklerden diyetisyenin görebildikleri (ortak + kendi). Plan kurarken satırlara bağlanır.</summary>
    Task<IReadOnlyList<FoodItem>> GetManyAsync(Guid dietitianId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyenin görebildiği tek besin (ortak ya da kendi); yoksa null.</summary>
    Task<FoodItem?> GetAsync(Guid dietitianId, Guid foodItemId, CancellationToken cancellationToken = default);

    /// <summary>Daha önce aynı dış kaynaktan aktarılmış kayıt (aynı kayıt iki kez aktarılmasın).</summary>
    Task<FoodItem?> GetByExternalIdAsync(Guid dietitianId, string sourceName, string externalId, CancellationToken cancellationToken = default);

    /// <summary>Besin bir planda ya da şablonda kullanılıyor mu? Kullanılıyorsa silinemez (FR-D2).</summary>
    Task<bool> IsInUseAsync(Guid foodItemId, CancellationToken cancellationToken = default);

    void Add(FoodItem food);

    void Remove(FoodItem food);
}
