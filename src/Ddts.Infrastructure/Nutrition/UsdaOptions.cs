namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// USDA FoodData Central ayarları ("Usda" bölümü). API anahtarı yalnızca sunucuda durur (NFR-G5):
/// yerelde user-secrets (Usda:ApiKey), Docker'da .env (USDA_API_KEY). Anahtar boşsa dış kaynak kullanılamaz sayılır.
/// </summary>
public sealed class UsdaOptions
{
    public const string SectionName = "Usda";

    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://api.nal.usda.gov/fdc/v1/";

    /// <summary>Zaman aşımı (ADR-0004: 3 sn).</summary>
    public int TimeoutSeconds { get; set; } = 3;

    /// <summary>Saatlik istek kotası (USDA sınırı IP başına saatte 1.000; biz 900'de dururuz).</summary>
    public int HourlyQuota { get; set; } = 900;
}
