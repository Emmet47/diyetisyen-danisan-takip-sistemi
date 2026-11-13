using Ddts.Domain.Common;

namespace Ddts.Domain.MealLogs;

/// <summary>
/// Öğün fotoğrafının bilgisi (değer nesnesi, Şekil 4.3). Dosyanın kendisi nesne deposunda (SeaweedFS),
/// burada yalnızca rastgele anahtarı, türü ve boyutu durur (BR-10, NFR-G3).
/// Veritabanında meal_logs tablosunun photo_key, photo_content_type, photo_size_bytes sütunları.
/// </summary>
public sealed record PhotoInfo
{
    /// <summary>En fazla 5 MB (5 × 1024 × 1024 bayt).</summary>
    public const int MaxSizeBytes = 5_242_880;

    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png"];

    // EF Core da bu kurucuyu kullanır (parametre adları özellik adlarıyla aynı).
    private PhotoInfo(string key, string contentType, int sizeBytes)
    {
        Key = key;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    public string Key { get; }

    public string ContentType { get; }

    public int SizeBytes { get; }

    /// <summary>
    /// Kuralları kontrol edip fotoğraf bilgisini oluşturur. Dosyanın gerçekten JPEG/PNG olduğunu
    /// (dosya imzası) servis katmanı yükleme sırasında ayrıca kontrol eder.
    /// </summary>
    public static PhotoInfo Create(string key, string contentType, int sizeBytes)
    {
        var cleanKey = Guard.Required(key, 200, "Fotoğraf anahtarı");

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new DomainException("Sadece JPEG veya PNG fotoğraf yüklenebilir.");
        }

        if (sizeBytes < 1 || sizeBytes > MaxSizeBytes)
        {
            throw new DomainException("Fotoğraf en fazla 5 MB olabilir.");
        }

        return new PhotoInfo(cleanKey, contentType, sizeBytes);
    }
}
