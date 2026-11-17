namespace Ddts.Application.Foods;

/// <summary>Gelen port: diyetisyenin kendi yerel besin kayıtları (FR-D2). Ortak kayıtlar düzenlenemez.</summary>
public interface IFoodService
{
    Task<Guid> CreateAsync(FoodCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid foodItemId, FoodCommand command, CancellationToken cancellationToken = default);

    /// <summary>Bir plan ya da şablonda kullanılan besin silinemez (409).</summary>
    Task DeleteAsync(Guid foodItemId, CancellationToken cancellationToken = default);
}
