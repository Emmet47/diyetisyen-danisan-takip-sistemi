using Ddts.Domain.Foods;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Yerel besin kayıtları (Şekil 4.10). Diyetisyen yalnızca ortak kayıtları (DietitianId boş)
/// ve kendi eklediği kayıtları görür.
/// </summary>
public interface IFoodItemRepository
{
    /// <summary>Verilen kimliklerden bu diyetisyenin görebildiği besinleri döner; görmediği kimlik listede olmaz.</summary>
    Task<IReadOnlyList<FoodItem>> GetManyAsync(Guid dietitianId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    void Add(FoodItem food);
}
