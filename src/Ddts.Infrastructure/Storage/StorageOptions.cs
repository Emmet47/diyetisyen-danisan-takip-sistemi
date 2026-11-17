namespace Ddts.Infrastructure.Storage;

/// <summary>
/// Fotoğraf deposu ayarları ("Storage" bölümü). Gizli anahtar yalnızca sunucuda (user-secrets / .env).
/// ServiceUrl: API'nin depoya ulaştığı adres (Docker içinde http://seaweedfs:8333).
/// PublicUrl: tarayıcı ve telefonun imzalı bağlantıyla ulaşacağı adres (yerelde http://localhost:8333);
/// boşsa ServiceUrl kullanılır. İmza adresin sunucu adını da kapsadığı için bağlantı bu adresle üretilir.
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string ServiceUrl { get; set; } = string.Empty;

    public string? PublicUrl { get; set; }

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Bucket { get; set; } = "meal-photos";

    public string Region { get; set; } = "us-east-1";
}
