using System.Collections.Concurrent;
using Ddts.Application.Abstractions.Storage;

namespace Ddts.IntegrationTests;

/// <summary>Testlerde SeaweedFS yerine kullanılır: fotoğrafları bellekte tutar, imzalı bağlantı taklidi üretir.</summary>
public sealed class InMemoryPhotoStorage : IPhotoStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _objects = new();

    public IReadOnlyCollection<string> Keys => _objects.Keys.ToArray();

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        _objects[key] = copy.ToArray();
    }

    public string GetPresignedUrl(string key, TimeSpan validFor) =>
        $"https://depo.test/meal-photos/{key}?X-Amz-Expires={(int)validFor.TotalSeconds}&X-Amz-Signature=test";

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        _objects.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
