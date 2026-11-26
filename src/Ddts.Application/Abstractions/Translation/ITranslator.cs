namespace Ddts.Application.Abstractions.Translation;

/// <summary>
/// Giden port (Şekil 4.4): kısa metinleri (besin adı, arama sorgusu) bir dilden diğerine çevirir.
/// Uygulaması DeepLTranslationAdapter. FoodSearchService, dili Türkçe olmayan besin kaynağına (USDA) giden
/// sorguyu Türkçeden çevirir, gelen besin adlarını da Türkçeye çevirir (ADR-0007).
/// Çeviri yapılamazsa (anahtar yok, kota dolu, servis yanıt vermiyor) hata fırlatmaz: metinleri olduğu gibi
/// döndürür. Arama çevirisiz devam eder (FR-D4).
/// </summary>
public interface ITranslator
{
    /// <summary>
    /// Metinleri sırasını koruyarak çevirir. Dil kodları ISO 639-1 ("tr", "en").
    /// Dönen liste her zaman girilen listeyle aynı uzunluktadır.
    /// </summary>
    Task<IReadOnlyList<string>> TranslateAsync(
        IReadOnlyList<string> texts,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);
}
