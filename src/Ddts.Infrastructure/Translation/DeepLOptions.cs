namespace Ddts.Infrastructure.Translation;

/// <summary>
/// DeepL çeviri ayarları ("DeepL" bölümü, ADR-0007). Ücretsiz plan (DeepL API Free) ayda 500.000 karakter verir.
/// API anahtarı yalnızca sunucuda durur (NFR-G5): yerelde user-secrets (DeepL:ApiKey), Docker'da .env (DEEPL_API_KEY).
/// Anahtar boşsa çeviri yapılmaz, arama sorgusu ve adlar olduğu gibi kalır.
/// </summary>
public sealed class DeepLOptions
{
    public const string SectionName = "DeepL";

    public string? ApiKey { get; set; }

    /// <summary>
    /// Boşsa anahtara göre seçilir: ücretsiz anahtarlar ":fx" ile biter ve api-free.deepl.com'a gider,
    /// diğerleri api.deepl.com'a.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>Zaman aşımı; çeviri beklenirken arama uzamasın diye kısa.</summary>
    public int TimeoutSeconds { get; set; } = 3;

    /// <summary>Aynı metnin çevirisi bu kadar saat bellekte tutulur (kotayı korumak için).</summary>
    public int CacheHours { get; set; } = 24;

    public string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(BaseUrl))
        {
            return BaseUrl;
        }

        var key = ApiKey?.Trim() ?? string.Empty;
        return key.Length == 0 || key.EndsWith(":fx", StringComparison.Ordinal)
            ? "https://api-free.deepl.com/v2/"
            : "https://api.deepl.com/v2/";
    }
}
