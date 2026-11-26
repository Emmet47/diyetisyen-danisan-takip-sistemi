namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// PlateRank ayarları ("PlateRank" bölümü, ADR-0007). PlateRank verisini salt-okunur bir REST ucundan verir;
/// anahtarı sitesinin SSS bölümünde herkese açık yazıyor. Yine de diğer anahtarlar gibi koda girmez, yalnızca
/// sunucuda durur (NFR-G5): yerelde user-secrets (PlateRank:ApiKey), Docker'da .env (PLATERANK_API_KEY).
/// Anahtar boşsa PlateRank kullanılamaz sayılır, arama diğer kaynaklarla devam eder.
/// </summary>
public sealed class PlateRankOptions
{
    public const string SectionName = "PlateRank";

    public string? ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://hpxnektzsrdhqzwwkfau.supabase.co/rest/v1/";

    /// <summary>Zaman aşımı; USDA ile aynı (ADR-0004: 3 sn).</summary>
    public int TimeoutSeconds { get; set; } = 3;

    /// <summary>Saatlik istek kotası. PlateRank bir sınır yazmıyor; ücretsiz bir servisi yormamak için koyduk.</summary>
    public int HourlyQuota { get; set; } = 600;
}
