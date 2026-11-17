using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Ddts.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Ddts.Infrastructure.Storage;

/// <summary>
/// IPhotoStorage'ın S3 uyumlu depo (SeaweedFS) adapter'ı (Şekil 4.4). Kova herkese kapalıdır; fotoğraf yalnızca
/// süreli imzalı bağlantıyla (presigned URL, 10 dakika) görüntülenir (FR-G2, NFR-G3).
/// Ayarlar ilk kullanımda okunur (testler kendi deposunu takabilsin diye).
/// </summary>
public sealed class S3PhotoStorageAdapter(IOptions<StorageOptions> options) : IPhotoStorage, IDisposable
{
    private readonly Lazy<AmazonS3Client> _client = new(() => CreateClient(options.Value, options.Value.ServiceUrl));
    private readonly Lazy<AmazonS3Client> _publicClient = new(() =>
        CreateClient(options.Value, string.IsNullOrWhiteSpace(options.Value.PublicUrl) ? options.Value.ServiceUrl : options.Value.PublicUrl));

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = options.Value.Bucket,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            // S3 uyumlu depolarda "aws-chunked" yükleme yerine düz gövde gönderilir.
            UseChunkEncoding = false,
        };

        await _client.Value.PutObjectAsync(request, cancellationToken);
    }

    public string GetPresignedUrl(string key, TimeSpan validFor)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.Value.PublicUrl) ? options.Value.ServiceUrl : options.Value.PublicUrl;

        return _publicClient.Value.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = options.Value.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(validFor),
            Protocol = baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? Protocol.HTTPS : Protocol.HTTP,
        });
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        await _client.Value.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = options.Value.Bucket, Key = key },
            cancellationToken);

    public void Dispose()
    {
        if (_client.IsValueCreated)
        {
            _client.Value.Dispose();
        }

        if (_publicClient.IsValueCreated)
        {
            _publicClient.Value.Dispose();
        }
    }

    private static AmazonS3Client CreateClient(StorageOptions storage, string serviceUrl)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl) || string.IsNullOrWhiteSpace(storage.SecretKey))
        {
            throw new InvalidOperationException(
                "Storage:ServiceUrl ya da Storage:SecretKey ayarı boş. Yerelde 'dotnet user-secrets', Docker'da .env (S3_SECRET_KEY).");
        }

        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true,                // http://depo:8333/kova/anahtar biçimi (SeaweedFS)
            AuthenticationRegion = storage.Region,
            // Yeni SDK'nın varsayılan bütünlük sağlama toplamları S3 uyumlu depolarda sorun çıkarabiliyor.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        return new AmazonS3Client(new BasicAWSCredentials(storage.AccessKey, storage.SecretKey), config);
    }
}
