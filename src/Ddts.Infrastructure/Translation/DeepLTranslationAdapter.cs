using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ddts.Application.Abstractions.Translation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ddts.Infrastructure.Translation;

/// <summary>
/// ITranslator'ın DeepL uygulaması (giden adapter, ADR-0007). POST /v2/translate ile birden çok metni tek
/// istekte çevirir; anahtar "Authorization: DeepL-Auth-Key ..." başlığıyla gider, istemcilere hiç gitmez (NFR-G5).
/// Çevrilen metinler bellekte tutulur, aynı arama ikinci kez kota harcamaz.
/// Anahtar yoksa, kota dolduysa (456), zaman aşımı ya da hata olursa metinler olduğu gibi döner; arama durmaz.
/// </summary>
public sealed class DeepLTranslationAdapter(
    HttpClient httpClient,
    IOptions<DeepLOptions> options,
    IMemoryCache cache,
    ILogger<DeepLTranslationAdapter> logger) : ITranslator
{
    // DeepL'e metnin ne olduğunu söyler; kısa besin adlarında doğru anlamı seçmesine yardım eder (ücret alınmaz).
    private const string Context = "Food and dish names in a nutrition database used by dietitians.";

    public async Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var results = texts.ToArray();
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey) || sourceLanguage == targetLanguage)
        {
            return results;
        }

        // Önce bellekteki çeviriler; kalanlar tek istekle sorulur.
        var missing = new List<int>();
        for (var i = 0; i < results.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(results[i]))
            {
                continue;
            }

            if (cache.TryGetValue(CacheKey(results[i], sourceLanguage, targetLanguage), out string? cached) && cached is not null)
            {
                results[i] = cached;
            }
            else
            {
                missing.Add(i);
            }
        }

        if (missing.Count == 0)
        {
            return results;
        }

        var translated = await RequestAsync(missing.Select(i => texts[i]).ToList(), sourceLanguage, targetLanguage, cancellationToken);
        if (translated is null)
        {
            return results;
        }

        var lifetime = TimeSpan.FromHours(options.Value.CacheHours);
        for (var j = 0; j < missing.Count; j++)
        {
            var index = missing[j];
            results[index] = translated[j];
            cache.Set(CacheKey(texts[index], sourceLanguage, targetLanguage), translated[j], lifetime);
        }

        return results;
    }

    /// <summary>DeepL isteği; başarısızsa null (ayrıntı yalnızca logda, anahtar loga yazılmaz).</summary>
    private async Task<IReadOnlyList<string>?> RequestAsync(
        IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "translate")
            {
                Content = JsonContent.Create(new DeepLRequest(
                    texts, SourceCode(sourceLanguage), TargetCode(targetLanguage), Context)),
            };
            request.Headers.TryAddWithoutValidation("Authorization", "DeepL-Auth-Key " + options.Value.ApiKey!.Trim());

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 403 anahtar hatalı, 429 çok sık istek, 456 aylık kota doldu
                logger.LogWarning("DeepL çevirisi başarısız: {Status}", (int)response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<DeepLResponse>(cancellationToken);
            if (body?.Translations is not { } translations || translations.Count != texts.Count)
            {
                logger.LogWarning("DeepL yanıtı beklenen biçimde değil.");
                return null;
            }

            return translations.Select((t, i) => string.IsNullOrWhiteSpace(t.Text) ? texts[i] : t.Text.Trim()).ToList();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException
                                          && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("DeepL isteği başarısız: {Error}", exception.GetType().Name);
            return null;
        }
    }

    private static string CacheKey(string text, string from, string to) => $"deepl:{from}>{to}:{text.Trim()}";

    // DeepL dil kodları: kaynakta "EN", hedefte İngilizce için "EN-US"; Türkçe her ikisinde "TR".
    private static string SourceCode(string language) => language.ToUpperInvariant();

    private static string TargetCode(string language) =>
        language.Equals("en", StringComparison.OrdinalIgnoreCase) ? "EN-US" : language.ToUpperInvariant();

    private sealed record DeepLRequest(
        [property: JsonPropertyName("text")] IReadOnlyList<string> Text,
        [property: JsonPropertyName("source_lang")] string SourceLang,
        [property: JsonPropertyName("target_lang")] string TargetLang,
        [property: JsonPropertyName("context")] string Context);

    private sealed record DeepLResponse(
        [property: JsonPropertyName("translations")] IReadOnlyList<DeepLTranslation>? Translations);

    private sealed record DeepLTranslation(
        [property: JsonPropertyName("text")] string? Text);
}
