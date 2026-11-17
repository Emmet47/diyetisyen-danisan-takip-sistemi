using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Foods;

namespace Ddts.Application.Foods;

/// <summary>
/// Yerel besin kayıtları (D modülü, FR-D2). Diyetisyen yalnızca kendi kayıtlarını düzenler/siler;
/// ortak (hazır yüklenen) kayıtları herkes kullanır ama kimse düzenleyemez (403).
/// </summary>
public sealed class FoodService(
    IFoodItemRepository foods,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IFoodService
{
    /// <summary>Elle girilen besinlerin kaynak adı.</summary>
    public const string LocalSourceName = "Yerel";

    public async Task<Guid> CreateAsync(FoodCommand command, CancellationToken cancellationToken = default)
    {
        var food = new FoodItem(
            DietitianId(),
            command.Name,
            command.PortionName,
            command.PortionGrams,
            command.EnergyKcalPer100g,
            command.ProteinPer100g,
            command.CarbsPer100g,
            command.FatPer100g,
            LocalSourceName,
            externalId: null,
            sourceUpdatedAt: null);

        foods.Add(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return food.Id;
    }

    public async Task UpdateAsync(Guid foodItemId, FoodCommand command, CancellationToken cancellationToken = default)
    {
        var food = await GetOwnFoodAsync(foodItemId, cancellationToken);

        food.Update(
            command.Name,
            command.PortionName,
            command.PortionGrams,
            command.EnergyKcalPer100g,
            command.ProteinPer100g,
            command.CarbsPer100g,
            command.FatPer100g);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid foodItemId, CancellationToken cancellationToken = default)
    {
        var food = await GetOwnFoodAsync(foodItemId, cancellationToken);

        if (await foods.IsInUseAsync(food.Id, cancellationToken))
        {
            throw new ConflictException("Bu besin bir planda ya da şablonda kullanılıyor, silinemez.");
        }

        // Danışanların kısıt listelerindeki satırlar veritabanında zincirleme silinir.
        foods.Remove(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<FoodItem> GetOwnFoodAsync(Guid foodItemId, CancellationToken cancellationToken)
    {
        var dietitianId = DietitianId();
        var food = await foods.GetAsync(dietitianId, foodItemId, cancellationToken)
            ?? throw new NotFoundException("Besin bulunamadı.");

        if (food.DietitianId != dietitianId)
        {
            throw new ForbiddenException("Ortak besin kayıtları düzenlenemez ve silinemez. Kendi kaydını ekleyebilirsin.");
        }

        return food;
    }

    private Guid DietitianId() =>
        currentUser.DietitianId ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");
}
