namespace Ddts.Application.Abstractions.Storage;

/// <summary>
/// Öğün fotoğrafları için nesne deposu portu (Şekil 4.4, FR-G2, NFR-G3). Depo herkese kapalıdır;
/// fotoğraf yalnızca süreli imzalı bağlantıyla görüntülenir. Adapter: S3PhotoStorageAdapter (SeaweedFS).
/// </summary>
public interface IPhotoStorage
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>validFor süresince geçerli, imzalı (presigned) görüntüleme bağlantısı.</summary>
    string GetPresignedUrl(string key, TimeSpan validFor);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
