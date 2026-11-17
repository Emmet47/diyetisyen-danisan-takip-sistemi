using Ddts.Application.Abstractions.Nutrition;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Foods;

namespace Ddts.Application.Foods;

/// <summary>
/// Besin arama ve içe aktarma (Şekil 4.9 ve 5.8). İki NutritionDataPort kullanır: yerel veritabanı ve dış kaynak
/// (USDA). Hangi adapter'ın takılacağı Program.cs'te seçilir (keyed DI). Dış kaynak çalışmazsa (FR-D4, NFR-E1)
/// arama 500 vermez: yerel sonuçlar "dış kaynağa ulaşılamıyor" uyarısıyla döner.
/// </summary>
public sealed class FoodSearchService(
    NutritionDataPort localSource,
    NutritionDataPort externalSource,
    IFoodItemRepository foods,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IFoodSearchService
{
    public const int ResultLimit = 20;
    public const string ExternalUnavailableMessage = "Dış kaynağa şu an ulaşılamıyor, yerel kayıtlar gösteriliyor.";

    public async Task<FoodSearchResult> SearchAsync(
        string? query, string? source, bool includeExternal, CancellationToken cancellationToken = default)
    {
        var cleanQuery = query?.Trim() ?? string.Empty;
        if (cleanQuery.Length > 100)
        {
            throw new Ddts.Domain.Common.DomainException("Arama metni en fazla 100 karakter olabilir.");
        }

        var onlyLocal = IsSource(source, FoodService.LocalSourceName);
        var onlyExternal = IsSource(source, "USDA");

        var local = await localSource.SearchAsync(cleanQuery, ResultLimit, cancellationToken);
        var items = local
            .Where(i => !onlyLocal || i.SourceName == FoodService.LocalSourceName)
            .Where(i => !onlyExternal || i.SourceName != FoodService.LocalSourceName)
            .ToList();

        if (!includeExternal || onlyLocal || cleanQuery.Length < 2)
        {
            return new FoodSearchResult(items, ExternalUnavailable: false, Message: null);
        }

        try
        {
            var external = await externalSource.SearchAsync(cleanQuery, ResultLimit, cancellationToken);

            // Daha önce aktarılmış dış kayıt zaten yerel listede: ikinci kez gösterilmez.
            var imported = items.Where(i => i.ExternalId is not null).Select(i => i.ExternalId).ToHashSet();
            items.AddRange(external.Where(e => !imported.Contains(e.ExternalId)));

            return new FoodSearchResult(items, ExternalUnavailable: false, Message: null);
        }
        catch (ExternalSourceUnavailableException)
        {
            // Ayrıntı adapter'da loglandı; kullanıcıya yalnızca genel mesaj gösterilir (Şekil 5.8).
            return new FoodSearchResult(items, ExternalUnavailable: true, ExternalUnavailableMessage);
        }
    }

    public async Task<Guid> ImportAsync(ImportFoodCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

        var externalId = command.ExternalId?.Trim();
        if (string.IsNullOrEmpty(externalId))
        {
            throw new Ddts.Domain.Common.DomainException("Aktarılacak besinin dış kaynak kimliği boş olamaz.");
        }

        var existing = await foods.GetByExternalIdAsync(dietitianId, externalSource.SourceName, externalId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        // Dış kaynağa ulaşılamazsa ExternalSourceUnavailableException → API 503 döner.
        var item = await externalSource.GetByExternalIdAsync(externalId, cancellationToken)
            ?? throw new NotFoundException("Dış kaynakta bu kimlikle besin bulunamadı.");

        var food = new FoodItem(
            dietitianId,
            string.IsNullOrWhiteSpace(command.Name) ? item.Name : command.Name,
            string.IsNullOrWhiteSpace(command.PortionName) ? item.PortionName : command.PortionName,
            command.PortionGrams ?? item.PortionGrams,
            item.EnergyKcalPer100g,
            item.ProteinPer100g,
            item.CarbsPer100g,
            item.FatPer100g,
            item.SourceName,
            item.ExternalId,
            item.RetrievedAt);

        foods.Add(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return food.Id;
    }

    private static bool IsSource(string? source, string name) =>
        !string.IsNullOrWhiteSpace(source) && source.Trim().StartsWith(name, StringComparison.OrdinalIgnoreCase);
}
