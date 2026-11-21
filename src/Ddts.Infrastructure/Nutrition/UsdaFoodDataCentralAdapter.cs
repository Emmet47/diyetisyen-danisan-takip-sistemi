using System.Globalization;
using Ddts.Application.Abstractions.Nutrition;
using Microsoft.Extensions.Logging;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// Adapter deseni (Şekil 4.9): USDA FoodData Central yanıtını (FdcFoodDto) sistemin standart NutritionItem
/// modeline çevirir. Zaman aşımı (3 sn), ağ/sunucu hatası, 429 ya da saatlik kota dolu ise
/// ExternalSourceUnavailableException fırlatır; FoodSearchService yerel sonuçlarla devam eder (Şekil 5.8).
/// Türkçe arama için TR→EN besin sözlüğü kullanır ("yulaf ezmesi" → "rolled oats"); sözlükte hiç kelime yoksa sorgu aynen gider.
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
    // Önce iki kelimelik ifadeler ("yulaf ezmesi" → "rolled oats"), sonra tek kelimeler aranır.
    private static readonly Dictionary<string, string> TurkishPhrases = new(StringComparer.Ordinal)
    {
        ["yulaf ezmesi"] = "rolled oats", ["tam buğday"] = "whole wheat", ["tavuk göğsü"] = "chicken breast",
        ["tavuk but"] = "chicken thigh", ["dana kıyma"] = "ground beef", ["kuzu eti"] = "lamb", ["dana eti"] = "beef",
        ["beyaz peynir"] = "feta cheese", ["kaşar peyniri"] = "cheddar cheese", ["lor peyniri"] = "ricotta cheese",
        ["süzme yoğurt"] = "greek yogurt", ["zeytin yağı"] = "olive oil", ["ayçiçek yağı"] = "sunflower oil",
        ["fındık ezmesi"] = "hazelnut butter", ["fıstık ezmesi"] = "peanut butter", ["kuru üzüm"] = "raisins",
        ["kuru kayısı"] = "dried apricots", ["kuru incir"] = "dried figs", ["kırmızı mercimek"] = "red lentils",
        ["yeşil mercimek"] = "green lentils", ["kuru fasulye"] = "white beans", ["barbunya fasulye"] = "cranberry beans",
        ["taze fasulye"] = "green beans", ["kırmızı et"] = "red meat", ["kırmızı biber"] = "red pepper",
        ["yeşil biber"] = "green pepper", ["sivri biber"] = "green pepper", ["dolma biber"] = "bell pepper",
        ["kuru soğan"] = "onion", ["yeşil soğan"] = "green onion", ["tatlı patates"] = "sweet potato",
        ["esmer pirinç"] = "brown rice", ["pirinç pilavı"] = "cooked rice", ["bulgur pilavı"] = "cooked bulgur",
        ["tam tahıllı"] = "whole grain", ["çavdar ekmeği"] = "rye bread", ["mısır gevreği"] = "corn flakes",
        ["ton balığı"] = "tuna", ["hindi füme"] = "smoked turkey", ["chia tohumu"] = "chia seeds",
        ["keten tohumu"] = "flaxseed", ["kabak çekirdeği"] = "pumpkin seeds", ["ay çekirdeği"] = "sunflower seeds",
        ["hindistan cevizi"] = "coconut", ["bitter çikolata"] = "dark chocolate", ["sütlü çikolata"] = "milk chocolate",
        ["badem sütü"] = "almond milk", ["soya sütü"] = "soy milk", ["yumurta akı"] = "egg white",
        ["yumurta sarısı"] = "egg yolk", ["portakal suyu"] = "orange juice", ["elma suyu"] = "apple juice",
    };

    private static readonly Dictionary<string, string> TurkishToEnglish = new(StringComparer.Ordinal)
    {
        // Tahıllar ve unlu yiyecekler
        ["yulaf"] = "oats", ["ekmek"] = "bread", ["tam"] = "whole", ["buğday"] = "wheat", ["pirinç"] = "rice",
        ["bulgur"] = "bulgur", ["makarna"] = "pasta", ["erişte"] = "noodles", ["un"] = "flour", ["mısır"] = "corn",
        ["patates"] = "potato", ["arpa"] = "barley", ["çavdar"] = "rye", ["kinoa"] = "quinoa", ["karabuğday"] = "buckwheat",
        ["simit"] = "bagel", ["lavaş"] = "flatbread", ["bisküvi"] = "biscuit", ["kraker"] = "crackers", ["galeta"] = "rusk",
        ["pilav"] = "rice", ["gevrek"] = "cereal", ["müsli"] = "muesli", ["granola"] = "granola", ["kepek"] = "bran",
        // Süt ürünleri ve yumurta
        ["yumurta"] = "egg", ["süt"] = "milk", ["yoğurt"] = "yogurt", ["peynir"] = "cheese", ["beyaz"] = "white",
        ["kaşar"] = "cheddar", ["lor"] = "ricotta", ["tulum"] = "cheese", ["labne"] = "cream cheese", ["krema"] = "cream",
        ["kefir"] = "kefir", ["ayran"] = "buttermilk", ["tereyağı"] = "butter", ["margarin"] = "margarine",
        ["mozzarella"] = "mozzarella", ["parmesan"] = "parmesan",
        // Yağlar
        ["zeytinyağı"] = "olive oil", ["yağ"] = "oil", ["zeytin"] = "olives", ["ayçiçek"] = "sunflower",
        // Et, tavuk, balık
        ["tavuk"] = "chicken", ["göğüs"] = "breast", ["göğsü"] = "breast", ["but"] = "thigh", ["kanat"] = "wing",
        ["hindi"] = "turkey", ["dana"] = "beef", ["sığır"] = "beef", ["et"] = "meat", ["kıyma"] = "ground beef",
        ["köfte"] = "meatballs", ["kuzu"] = "lamb", ["ciğer"] = "liver", ["sucuk"] = "sausage", ["sosis"] = "sausage",
        ["pastırma"] = "pastrami", ["salam"] = "salami", ["balık"] = "fish", ["somon"] = "salmon", ["ton"] = "tuna",
        ["levrek"] = "sea bass", ["çipura"] = "sea bream", ["hamsi"] = "anchovy", ["sardalya"] = "sardines",
        ["uskumru"] = "mackerel", ["karides"] = "shrimp", ["midye"] = "mussels", ["kalamar"] = "squid",
        // Baklagiller
        ["mercimek"] = "lentils", ["nohut"] = "chickpeas", ["fasulye"] = "beans", ["barbunya"] = "cranberry beans",
        ["bakla"] = "fava beans", ["bezelye"] = "peas", ["soya"] = "soybeans", ["börülce"] = "black-eyed peas",
        ["humus"] = "hummus", ["kuru"] = "dry", ["kırmızı"] = "red", ["yeşil"] = "green", ["sarı"] = "yellow",
        ["siyah"] = "black", ["esmer"] = "brown", ["taze"] = "fresh",
        // Meyveler
        ["elma"] = "apple", ["armut"] = "pear", ["muz"] = "banana", ["portakal"] = "orange", ["mandalina"] = "tangerine",
        ["greyfurt"] = "grapefruit", ["çilek"] = "strawberries", ["üzüm"] = "grapes", ["kiraz"] = "cherries",
        ["vişne"] = "sour cherries", ["karpuz"] = "watermelon", ["kavun"] = "melon", ["şeftali"] = "peach",
        ["kayısı"] = "apricot", ["erik"] = "plum", ["incir"] = "figs", ["nar"] = "pomegranate", ["ayva"] = "quince",
        ["kivi"] = "kiwi", ["ananas"] = "pineapple", ["mango"] = "mango", ["hurma"] = "dates", ["limon"] = "lemon",
        ["böğürtlen"] = "blackberries", ["ahududu"] = "raspberries", ["yaban"] = "wild", ["mersini"] = "blueberries",
        ["dut"] = "mulberries", ["avokado"] = "avocado", ["meyve"] = "fruit",
        // Sebzeler
        ["domates"] = "tomato", ["salatalık"] = "cucumber", ["biber"] = "pepper", ["soğan"] = "onion",
        ["sarımsak"] = "garlic", ["havuç"] = "carrot", ["ıspanak"] = "spinach", ["marul"] = "lettuce",
        ["roka"] = "arugula", ["maydanoz"] = "parsley", ["dereotu"] = "dill", ["nane"] = "mint", ["lahana"] = "cabbage",
        ["brokoli"] = "broccoli", ["karnabahar"] = "cauliflower", ["kabak"] = "squash", ["patlıcan"] = "eggplant",
        ["mantar"] = "mushrooms", ["pırasa"] = "leek", ["kereviz"] = "celery", ["pancar"] = "beet", ["turp"] = "radish",
        ["enginar"] = "artichoke", ["bamya"] = "okra", ["semizotu"] = "purslane", ["pazı"] = "chard",
        ["kuşkonmaz"] = "asparagus", ["sebze"] = "vegetables", ["salata"] = "salad",
        // Kuruyemiş ve tohumlar
        ["ceviz"] = "walnuts", ["fındık"] = "hazelnuts", ["badem"] = "almonds", ["fıstık"] = "peanuts",
        ["antep"] = "pistachio", ["kaju"] = "cashew", ["leblebi"] = "roasted chickpeas", ["susam"] = "sesame",
        ["tahin"] = "tahini", ["pekmez"] = "molasses", ["tohum"] = "seeds", ["çekirdek"] = "seeds", ["yer"] = "ground",
        // Tatlandırıcılar, içecekler, diğer
        ["bal"] = "honey", ["şeker"] = "sugar", ["reçel"] = "jam", ["çikolata"] = "chocolate", ["kakao"] = "cocoa",
        ["dondurma"] = "ice cream", ["kahve"] = "coffee", ["çay"] = "tea", ["su"] = "water", ["suyu"] = "juice",
        ["meyvesuyu"] = "juice", ["kola"] = "cola", ["tuz"] = "salt", ["sirke"] = "vinegar", ["ketçap"] = "ketchup",
        ["mayonez"] = "mayonnaise", ["hardal"] = "mustard", ["çorba"] = "soup", ["çorbası"] = "soup",
        // Pişirme biçimleri
        ["haşlanmış"] = "boiled", ["haşlama"] = "boiled", ["kızarmış"] = "fried", ["kızartma"] = "fried",
        ["ızgara"] = "grilled", ["fırında"] = "baked", ["fırın"] = "baked", ["çiğ"] = "raw", ["pişmiş"] = "cooked",
        ["kavrulmuş"] = "roasted", ["füme"] = "smoked", ["konserve"] = "canned", ["dondurulmuş"] = "frozen",
        ["yağsız"] = "nonfat", ["yarım"] = "reduced fat", ["light"] = "light", ["tuzsuz"] = "unsalted",
    };

    // Sözlükte bulunmayan kelimede denenecek yaygın Türkçe ekler (uzundan kısaya).
    private static readonly string[] TurkishSuffixes =
        ["leri", "ları", "ler", "lar", "si", "sı", "su", "sü", "li", "lı", "lu", "lü", "i", "ı", "u", "ü"];

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

    /// <summary>
    /// Türkçe sorguyu USDA'nın anlayacağı İngilizceye çevirir. Önce iki kelimelik ifadeler ("tavuk göğsü"),
    /// sonra tek kelimeler, bulunamazsa ekleri atılmış hali ("elmalar" → "elma") aranır. En az bir kelime
    /// çevrildiyse çevrilemeyen kelimeler atılır ("yulaf ezmesi lezzetli" → "rolled oats"); hiçbiri
    /// çevrilemediyse sorgu aynen gider (kullanıcı İngilizce yazmış olabilir).
    /// </summary>
    private static string TranslateQuery(string query)
    {
        var words = query.Trim().ToLower(Turkish)
            .Split([' ', ',', '.', '-', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries);
        var english = new List<string>();

        for (var i = 0; i < words.Length; i++)
        {
            if (i + 1 < words.Length && TurkishPhrases.TryGetValue($"{words[i]} {words[i + 1]}", out var phrase))
            {
                english.Add(phrase);
                i++;
                continue;
            }

            // Çevrilemeyen Türkçe kelime USDA aramasını bozuyor ("oats ezmesi" sonuç vermez): atlanır.
            if (Lookup(words[i]) is { } word)
            {
                english.Add(word);
            }
        }

        return english.Count == 0 ? query.Trim() : string.Join(' ', english);

        // Kelimeyi sözlükte arar; yoksa yaygın ekleri atıp tekrar dener ("elmalar" → "elma").
        // (Yerel fonksiyon: sınıfa yeni üye eklenmesin, P-5 diyagramı değişmesin.)
        static string? Lookup(string word)
        {
            if (TurkishToEnglish.TryGetValue(word, out var english))
            {
                return english;
            }

            foreach (var suffix in TurkishSuffixes)
            {
                if (word.Length > suffix.Length + 2 && word.EndsWith(suffix, StringComparison.Ordinal)
                    && TurkishToEnglish.TryGetValue(word[..^suffix.Length], out english))
                {
                    return english;
                }
            }

            return null;
        }
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
