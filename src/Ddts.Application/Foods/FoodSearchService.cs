using Ddts.Application.Abstractions.Nutrition;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Abstractions.Translation;
using Ddts.Application.Common;
using Ddts.Domain.Common;
using Ddts.Domain.Foods;

namespace Ddts.Application.Foods;

/// <summary>
/// Besin arama ve içe aktarma (Şekil 4.9 ve 5.8). Yerel veritabanı ve dış kaynaklar (PlateRank, USDA) aynı
/// NutritionDataPort'u uygular; hangi adapter'ların takılacağı Program.cs'te seçilir (keyed DI).
/// Dili Türkçe olmayan kaynağa (USDA) giden sorgu ITranslator ile çevrilir, gelen adlar Türkçeye çevrilir (ADR-0007).
/// Bir dış kaynak çalışmazsa (FR-D4, NFR-E1) arama 500 vermez: diğer sonuçlar uyarıyla döner.
/// </summary>
public sealed class FoodSearchService(
    NutritionDataPort localSource,
    IReadOnlyList<NutritionDataPort> externalSources,
    ITranslator translator,
    IFoodItemRepository foods,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IFoodSearchService
{
    /// <summary>Her dış kaynaktan en fazla bu kadar sonuç istenir (kota için az tutuluyor).</summary>
    public const int ResultLimit = 20;

    /// <summary>Yerel kayıtlarda en fazla bu kadar sonuç döner; arama kutusu boşken bütün liste görünsün diye geniş.</summary>
    public const int LocalResultLimit = 100;

    /// <summary>Hiçbir dış kaynağa ulaşılamadığında gösterilen mesaj.</summary>
    public const string ExternalUnavailableMessage = "Dış kaynağa şu an ulaşılamıyor, yerel kayıtlar gösteriliyor.";

    /// <summary>Sistemin dili; besin adları bu dilde gösterilir.</summary>
    private const string Turkish = "tr";

    public async Task<FoodSearchResult> SearchAsync(
        string? query, string? source, bool includeExternal, CancellationToken cancellationToken = default)
    {
        var cleanQuery = query?.Trim() ?? string.Empty;
        if (cleanQuery.Length > 100)
        {
            throw new DomainException("Arama metni en fazla 100 karakter olabilir.");
        }

        // source boşsa hepsi; doluysa adı bununla başlayan kaynak ("Yerel", "PlateRank", "USDA").
        var local = await localSource.SearchAsync(cleanQuery, LocalResultLimit, cancellationToken);
        var items = local.Where(i => IsSource(source, i.SourceName)).ToList();

        var selected = IsSource(source, localSource.SourceName) && !string.IsNullOrWhiteSpace(source)
            ? []
            : externalSources.Where(s => IsSource(source, s.SourceName)).ToList();

        if (!includeExternal || selected.Count == 0 || cleanQuery.Length < 2)
        {
            return new FoodSearchResult(items, ExternalUnavailable: false, Message: null);
        }

        // Kaynaklar birbirini beklemesin: aynı anda sorulur, her biri kendi hatasını yakalar.
        var results = await Task.WhenAll(selected.Select(s => SearchExternalAsync(s, cleanQuery, cancellationToken)));

        // Daha önce aktarılmış dış kayıt zaten yerel listede: ikinci kez gösterilmez.
        var imported = items
            .Where(i => i.ExternalId is not null)
            .Select(i => (i.SourceName, i.ExternalId))
            .ToHashSet();

        var failed = new List<string>();
        for (var i = 0; i < selected.Count; i++)
        {
            if (results[i] is { } found)
            {
                items.AddRange(found.Where(e => !imported.Contains((e.SourceName, e.ExternalId))));
            }
            else
            {
                failed.Add(selected[i].SourceName);
            }
        }

        if (failed.Count == 0)
        {
            return new FoodSearchResult(items, ExternalUnavailable: false, Message: null);
        }

        // Ayrıntı adapter'da loglandı; kullanıcıya yalnızca genel mesaj gösterilir (Şekil 5.8).
        var message = failed.Count == selected.Count
            ? ExternalUnavailableMessage
            : $"{string.Join(", ", failed)} kaynağına şu an ulaşılamıyor, diğer sonuçlar gösteriliyor.";
        return new FoodSearchResult(items, ExternalUnavailable: true, message);
    }

    public async Task<Guid> ImportAsync(ImportFoodCommand command, CancellationToken cancellationToken = default)
    {
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

        var externalId = command.ExternalId?.Trim();
        if (string.IsNullOrEmpty(externalId))
        {
            throw new DomainException("Aktarılacak besinin dış kaynak kimliği boş olamaz.");
        }

        var source = FindSource(command.Source);

        var existing = await foods.GetByExternalIdAsync(dietitianId, source.SourceName, externalId, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        // Dış kaynağa ulaşılamazsa ExternalSourceUnavailableException → API 503 döner.
        var item = await source.GetByExternalIdAsync(externalId, cancellationToken)
            ?? throw new NotFoundException("Dış kaynakta bu kimlikle besin bulunamadı.");

        var name = string.IsNullOrWhiteSpace(command.Name)
            ? (await TranslateNamesAsync(source, [item.Name], cancellationToken))[0]
            : command.Name;

        // Porsiyon başına gelen değerler (PlateRank) diyetisyenin girdiği gramla 100 g'a çevrilir.
        var grams = command.PortionGrams
            ?? (item.ValuesPerPortion
                ? throw new DomainException("Bu kaynak porsiyonun gramını vermiyor; aktarırken porsiyon gramını gir.")
                : item.PortionGrams);
        if (item.ValuesPerPortion && grams <= 0m)
        {
            throw new DomainException("Porsiyon (g) 0'dan büyük olmalı.");
        }

        var factor = item.ValuesPerPortion ? 100m / grams : 1m;

        var food = new FoodItem(
            dietitianId,
            name,
            string.IsNullOrWhiteSpace(command.PortionName) ? item.PortionName : command.PortionName,
            grams,
            Per100g(item.EnergyKcalPer100g, factor),
            Per100g(item.ProteinPer100g, factor),
            Per100g(item.CarbsPer100g, factor),
            Per100g(item.FatPer100g, factor),
            item.SourceName,
            item.ExternalId,
            item.RetrievedAt);

        foods.Add(food);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return food.Id;
    }

    /// <summary>
    /// Tek bir dış kaynakta arar; kaynak kullanılamazsa null döner. Dili Türkçe değilse sorgu o dile,
    /// gelen adlar Türkçeye çevrilir (çeviri yapılamazsa metinler olduğu gibi kalır).
    /// </summary>
    private async Task<IReadOnlyList<NutritionItem>?> SearchExternalAsync(
        NutritionDataPort source, string query, CancellationToken cancellationToken)
    {
        try
        {
            var sourceQuery = source.LanguageCode == Turkish
                ? query
                : (await translator.TranslateAsync([query], Turkish, source.LanguageCode, cancellationToken))[0];

            var found = await source.SearchAsync(sourceQuery, ResultLimit, cancellationToken);
            if (found.Count == 0 || source.LanguageCode == Turkish)
            {
                return found;
            }

            var names = await TranslateNamesAsync(source, found.Select(f => f.Name).ToList(), cancellationToken);
            return found.Select((f, i) => f with { Name = names[i] }).ToList();
        }
        catch (ExternalSourceUnavailableException)
        {
            return null;
        }
    }

    /// <summary>Kaynağın dilindeki adları Türkçeye çevirir; ad sütunu 150 karakter olduğu için kısaltılır.</summary>
    private async Task<IReadOnlyList<string>> TranslateNamesAsync(
        NutritionDataPort source, IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (source.LanguageCode == Turkish)
        {
            return names;
        }

        var translated = await translator.TranslateAsync(names, source.LanguageCode, Turkish, cancellationToken);
        return translated.Select((name, i) => string.IsNullOrWhiteSpace(name) ? names[i] : Shorten(name.Trim())).ToList();
    }

    /// <summary>İstekteki kaynak adına göre dış kaynağı bulur; boşsa USDA (eski istemciler kaynak göndermiyordu).</summary>
    private NutritionDataPort FindSource(string? source)
    {
        var name = string.IsNullOrWhiteSpace(source) ? "USDA" : source;
        return externalSources.FirstOrDefault(s => IsSource(name, s.SourceName))
            ?? throw new DomainException("Bilinmeyen besin kaynağı.");
    }

    private static decimal Per100g(decimal value, decimal factor) => Math.Round(value * factor, 1);

    private static string Shorten(string name) => name.Length > 150 ? name[..150] : name;

    /// <summary>source boşsa her kaynak uyar; doluysa kaynak adı bununla başlamalı (büyük/küçük harf fark etmez).</summary>
    private static bool IsSource(string? source, string sourceName) =>
        string.IsNullOrWhiteSpace(source) || sourceName.StartsWith(source.Trim(), StringComparison.OrdinalIgnoreCase);
}
