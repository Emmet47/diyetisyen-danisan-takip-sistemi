using System.Globalization;
using Ddts.Application.Abstractions.Nutrition;
using Microsoft.Extensions.Logging;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// Adapter deseni (Şekil 4.9): USDA FoodData Central yanıtını (FdcFoodDto) sistemin standart NutritionItem
/// modeline çevirir. Zaman aşımı (3 sn), ağ/sunucu hatası, 429 ya da saatlik kota dolu ise
/// ExternalSourceUnavailableException fırlatır; FoodSearchService yerel sonuçlarla devam eder (Şekil 5.8).
/// Türkçe arama için küçük bir TR→EN besin sözlüğü kullanır ("yulaf" → "oats"); sözlükte olmayan kelime aynen gider.
/// </summary>
public sealed class UsdaFoodDataCentralAdapter(
    UsdaFdcClient client,
    ApiQuotaCounter quota,
    TimeProvider timeProvider,
    ILogger<UsdaFoodDataCentralAdapter> logger) : NutritionDataPort
{
    private const string UnavailableMessage = "USDA FoodData Central şu an kullanılamıyor.";

    // USDA besin öğesi kimlikleri (nutrient id) ve eski numaraları (nutrient number)
    private static readonly (int Id, string Number)[] EnergyKcal = [(1008, "208"), (2047, "957"), (2048, "958")];
    private static readonly (int Id, string Number) Protein = (1003, "203");
    private static readonly (int Id, string Number) Fat = (1004, "204");
    private static readonly (int Id, string Number) Carbs = (1005, "205");

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    // TR→EN besin sözlüğü (13 Kasım kararı): yeni bağımlılık eklemeden Türkçe aramayı mümkün kılar.
    private static readonly Dictionary<string, string> TurkishToEnglish = new(StringComparer.Ordinal)
    {
        ["yulaf"] = "oats", ["ekmek"] = "bread", ["tam"] = "whole", ["buğday"] = "wheat", ["pirinç"] = "rice",
        ["bulgur"] = "bulgur", ["makarna"] = "pasta", ["un"] = "flour", ["mısır"] = "corn", ["patates"] = "potato",
        ["yumurta"] = "egg", ["süt"] = "milk", ["yoğurt"] = "yogurt", ["peynir"] = "cheese", ["beyaz"] = "white",
        ["kaşar"] = "cheddar", ["tereyağı"] = "butter", ["zeytinyağı"] = "olive oil", ["yağ"] = "oil", ["zeytin"] = "olives",
        ["tavuk"] = "chicken", ["göğüs"] = "breast", ["but"] = "thigh", ["hindi"] = "turkey", ["dana"] = "beef",
        ["kıyma"] = "ground beef", ["kuzu"] = "lamb", ["balık"] = "fish", ["somon"] = "salmon", ["ton"] = "tuna",
        ["mercimek"] = "lentils", ["kırmızı"] = "red", ["yeşil"] = "green", ["nohut"] = "chickpeas", ["fasulye"] = "beans",
        ["kuru"] = "dry", ["elma"] = "apple", ["armut"] = "pear", ["muz"] = "banana", ["portakal"] = "orange",
        ["mandalina"] = "tangerine", ["çilek"] = "strawberries", ["üzüm"] = "grapes", ["kiraz"] = "cherries",
        ["karpuz"] = "watermelon", ["kavun"] = "melon", ["şeftali"] = "peach", ["kayısı"] = "apricot", ["incir"] = "figs",
        ["limon"] = "lemon", ["domates"] = "tomato", ["salatalık"] = "cucumber", ["biber"] = "pepper", ["soğan"] = "onion",
        ["sarımsak"] = "garlic", ["havuç"] = "carrot", ["ıspanak"] = "spinach", ["marul"] = "lettuce", ["lahana"] = "cabbage",
        ["brokoli"] = "broccoli", ["karnabahar"] = "cauliflower", ["kabak"] = "squash", ["patlıcan"] = "eggplant",
        ["mantar"] = "mushrooms", ["ceviz"] = "walnuts", ["fındık"] = "hazelnuts", ["badem"] = "almonds",
        ["fıstık"] = "peanuts", ["yer"] = "ground", ["bal"] = "honey", ["şeker"] = "sugar", ["reçel"] = "jam",
        ["kahve"] = "coffee", ["çay"] = "tea", ["avokado"] = "avocado", ["kinoa"] = "quinoa",
    };

    public string SourceName => "USDA FoodData Central";

    public async Task<IReadOnlyList<NutritionItem>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
    {
        var foods = await CallAsync(c => c.SearchFoodsAsync(TranslateQuery(query), limit, cancellationToken), cancellationToken);
        var retrievedAt = timeProvider.GetUtcNow().UtcDateTime;
        return foods.Select(f => Map(f, retrievedAt)).ToList();
    }

    public async Task<NutritionItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(externalId, NumberStyles.None, CultureInfo.InvariantCulture, out var fdcId))
        {
            return null;
        }

        var food = await CallAsync(c => c.GetFoodAsync(fdcId, cancellationToken), cancellationToken);
        return food is null ? null : Map(food, timeProvider.GetUtcNow().UtcDateTime);
    }

    /// <summary>Türkçe sorgudaki bilinen kelimeleri İngilizceye çevirir; bilinmeyenler aynen kalır.</summary>
    private static string TranslateQuery(string query)
    {
        var words = query.Trim().ToLower(Turkish).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(w => TurkishToEnglish.TryGetValue(w, out var english) ? english : w));
    }

    /// <summary>FdcFoodDto → NutritionItem. Değerler 100 g içindir; veritabanı sınırlarına göre yuvarlanır.</summary>
    private static NutritionItem Map(FdcFoodDto dto, DateTime retrievedAt)
    {
        var protein = Clamp(Find(dto, Protein) ?? 0m, 100m);
        var fat = Clamp(Find(dto, Fat) ?? 0m, 100m);
        var carbs = Clamp(Find(dto, Carbs) ?? 0m, 100m);

        // Makro toplamı yuvarlama yüzünden 100 g'ı birkaç ondalık aşabilir: orantılı küçültülür.
        var macroTotal = protein + fat + carbs;
        if (macroTotal > 100m)
        {
            var factor = 100m / macroTotal;
            protein = Math.Floor(protein * factor * 10m) / 10m;
            fat = Math.Floor(fat * factor * 10m) / 10m;
            carbs = Math.Floor(carbs * factor * 10m) / 10m;
        }

        var energy = EnergyKcal.Select(key => Find(dto, key, kcalOnly: true)).FirstOrDefault(v => v is not null)
            ?? 4m * protein + 4m * carbs + 9m * fat;

        var name = string.IsNullOrWhiteSpace(dto.Description) ? $"USDA {dto.FdcId}" : dto.Description.Trim();

        return new NutritionItem(
            Id: null,
            Name: name.Length > 150 ? name[..150] : name,
            PortionName: "100 g",
            PortionGrams: 100m,
            EnergyKcalPer100g: Clamp(energy, 900m),
            ProteinPer100g: protein,
            CarbsPer100g: carbs,
            FatPer100g: fat,
            SourceName: "USDA FoodData Central",
            ExternalId: dto.FdcId.ToString(CultureInfo.InvariantCulture),
            RetrievedAt: retrievedAt);
    }

    private static decimal? Find(FdcFoodDto dto, (int Id, string Number) key, bool kcalOnly = false) => dto.FoodNutrients
        .Where(n => n.Id == key.Id || n.Number == key.Number)
        .Where(n => !kcalOnly || string.Equals(n.UnitName, "KCAL", StringComparison.OrdinalIgnoreCase))
        .Select(n => n.Amount)
        .FirstOrDefault(a => a is not null);

    private static decimal Clamp(decimal value, decimal max) => Math.Round(Math.Clamp(value, 0m, max), 1);

    /// <summary>Kota, zaman aşımı ve hata denetimi tek yerde: her sorun ExternalSourceUnavailableException olur.</summary>
    private async Task<T> CallAsync<T>(Func<UsdaFdcClient, Task<T>> call, CancellationToken cancellationToken)
    {
        if (!client.IsConfigured)
        {
            logger.LogInformation("USDA API anahtarı ayarlı değil (Usda:ApiKey); dış kaynak atlandı.");
            throw new ExternalSourceUnavailableException(UnavailableMessage);
        }

        if (!quota.TryConsume())
        {
            logger.LogWarning("USDA saatlik istek kotası doldu.");
            throw new ExternalSourceUnavailableException(UnavailableMessage);
        }

        try
        {
            return await call(client);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
                                          && !cancellationToken.IsCancellationRequested)
        {
            // Ayrıntı (durum kodu, zaman aşımı) yalnızca logda; API anahtarı loga yazılmaz.
            logger.LogWarning("USDA isteği başarısız: {Error}", exception.GetType().Name + " " + (exception as HttpRequestException)?.StatusCode);
            throw new ExternalSourceUnavailableException(UnavailableMessage, exception);
        }
    }
}
