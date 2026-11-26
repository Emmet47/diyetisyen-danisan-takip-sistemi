using System.Text.Json;
using Ddts.Application.Abstractions.Nutrition;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// Adapter deseni (Şekil 4.9): PlateRank yanıtını (PlateRankDishDto) standart NutritionItem modeline çevirir.
/// PlateRank'te Türk yemekleri Türkçe adlarıyla var (USDA'da yok); adlar Türkçe olduğu için çeviri gerekmez.
/// Değerler porsiyon başınadır: porsiyon gramı biliniyorsa 100 g'a çevrilir, bilinmiyorsa (Türk yemeklerinin
/// hepsi) ValuesPerPortion = true döner ve gram aktarırken diyetisyenden istenir (ADR-0007).
/// Hata, zaman aşımı, 429 ya da kota dolu ise ExternalSourceUnavailableException fırlatır (Şekil 5.8).
/// </summary>
public sealed class PlateRankAdapter(
    PlateRankClient client,
    [FromKeyedServices("platerank")] ApiQuotaCounter quota,
    TimeProvider timeProvider,
    ILogger<PlateRankAdapter> logger) : NutritionDataPort
{
    private const string UnavailableMessage = "PlateRank şu an kullanılamıyor.";

    public string SourceName => "PlateRank";

    public string LanguageCode => "tr";

    public async Task<IReadOnlyList<NutritionItem>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var dishes = await CallAsync(c => c.SearchDishesAsync(query, limit, cancellationToken), cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return dishes.Select(d => Map(d, now)).OfType<NutritionItem>().ToList();
    }

    public async Task<NutritionItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(externalId, out var id))
        {
            return null;
        }

        var dish = await CallAsync(c => c.GetDishAsync(id, cancellationToken), cancellationToken);
        return dish is null ? null : Map(dish, timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>
    /// PlateRankDishDto → NutritionItem. Adın sonundaki parantez porsiyon adı olur ("Baklava (2 dilim)" →
    /// "2 dilim"), yoksa "1 porsiyon". Güncellik bilgisi olarak satırın son güncellenme zamanı kullanılır.
    /// Enerjisi olmayan satır atlanır.
    /// </summary>
    private static NutritionItem? Map(PlateRankDishDto dto, DateTime now)
    {
        if (dto.EnergyKcal is not { } energy || string.IsNullOrWhiteSpace(dto.Name))
        {
            return null;
        }

        var name = dto.Name.Trim();
        var portionName = PortionNameOf(name);
        var protein = dto.Protein ?? 0m;
        var carbs = dto.Carbs ?? 0m;
        var fat = dto.Fat ?? 0m;
        var retrievedAt = dto.UpdatedAt?.UtcDateTime ?? now;

        if (dto.PortionGrams is > 0m and var grams)
        {
            // Porsiyon gramı belli: değerler 100 g'a çevrilir, USDA ile aynı sınırlar uygulanır.
            var factor = 100m / grams;
            var (p, c, f) = FitMacros(protein * factor, carbs * factor, fat * factor);
            return new NutritionItem(
                null, Shorten(name, 150), portionName, Math.Round(grams, 1),
                Round(Math.Min(energy * factor, 900m)), p, c, f,
                "PlateRank", dto.Id.ToString("D"), retrievedAt);
        }

        // Porsiyon gramı yok: değerler 1 porsiyon içindir, aktarırken gram istenir.
        return new NutritionItem(
            null, Shorten(name, 150), portionName, 0m,
            Round(energy), Round(protein), Round(carbs), Round(fat),
            "PlateRank", dto.Id.ToString("D"), retrievedAt, ValuesPerPortion: true);
    }

    private static string PortionNameOf(string name)
    {
        var open = name.LastIndexOf('(');
        if (name.EndsWith(')') && open > 0)
        {
            var portion = name[(open + 1)..^1].Trim();
            if (portion.Length is > 0 and <= 50)
            {
                return portion;
            }
        }

        return "1 porsiyon";
    }

    /// <summary>100 g'daki makro toplamı yuvarlama yüzünden 100'ü geçerse orantılı küçültülür.</summary>
    private static (decimal Protein, decimal Carbs, decimal Fat) FitMacros(decimal protein, decimal carbs, decimal fat)
    {
        protein = Math.Clamp(protein, 0m, 100m);
        carbs = Math.Clamp(carbs, 0m, 100m);
        fat = Math.Clamp(fat, 0m, 100m);

        var total = protein + carbs + fat;
        if (total <= 100m)
        {
            return (Round(protein), Round(carbs), Round(fat));
        }

        var factor = 100m / total;
        return (Math.Floor(protein * factor * 10m) / 10m, Math.Floor(carbs * factor * 10m) / 10m, Math.Floor(fat * factor * 10m) / 10m);
    }

    private static decimal Round(decimal value) => Math.Round(Math.Max(value, 0m), 1);

    private static string Shorten(string text, int max) => text.Length > max ? text[..max] : text;

    /// <summary>Kota, zaman aşımı ve hata denetimi tek yerde: her sorun ExternalSourceUnavailableException olur.</summary>
    private async Task<T> CallAsync<T>(Func<PlateRankClient, Task<T>> call, CancellationToken cancellationToken)
    {
        if (!client.IsConfigured)
        {
            logger.LogInformation("PlateRank anahtarı ayarlı değil (PlateRank:ApiKey); kaynak atlandı.");
            throw new ExternalSourceUnavailableException(UnavailableMessage);
        }

        if (!quota.TryConsume())
        {
            logger.LogWarning("PlateRank saatlik istek kotası doldu.");
            throw new ExternalSourceUnavailableException(UnavailableMessage);
        }

        try
        {
            return await call(client);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException
                                          && !cancellationToken.IsCancellationRequested)
        {
            // Ayrıntı yalnızca logda; anahtar loga yazılmaz.
            logger.LogWarning("PlateRank isteği başarısız: {Error}", exception.GetType().Name + " " + (exception as HttpRequestException)?.StatusCode);
            throw new ExternalSourceUnavailableException(UnavailableMessage, exception);
        }
    }
}
