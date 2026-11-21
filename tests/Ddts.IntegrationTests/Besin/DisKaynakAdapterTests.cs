using System.Net;
using System.Text;
using Ddts.Application.Abstractions.Nutrition;
using Ddts.Infrastructure.Nutrition;
using Ddts.Infrastructure.Translation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Ddts.IntegrationTests.Besin;

/// <summary>
/// Dış kaynak adapter'ları (Şekil 4.9, ADR-0007): PlateRank yanıtının NutritionItem'a çevrilmesi ve DeepL çevirisi.
/// Gerçek servislere gidilmez: HttpClient'a sahte bir handler takılır, istek ve yanıt test içinde yazılır.
/// Örnek değerler uydurmadır.
/// </summary>
public sealed class DisKaynakAdapterTests
{
    private const string PlateRankAdresi = "https://platerank.test/rest/v1/";

    private static CancellationToken Iptal => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PlateRank_porsiyon_grami_yoksa_degerler_porsiyon_basina_doner()
    {
        var handler = new SahteHandler(HttpStatusCode.OK, """
            [{"id":"0b6f3c2e-1d1a-4c55-9a0e-3f2a1b9c8d7e","ad":"Baklava (2 dilim)","mutfak":"tr","kcal":330,"protein":5,
              "karbonhidrat":29,"yag":22,"porsiyon_g":null,"kaynak":null,"updated_at":"2026-10-21T13:58:35+00:00"}]
            """);
        var adapter = PlateRank(handler, anahtar: "deneme-anahtari");

        var sonuc = await adapter.SearchAsync("baklava", 20, Iptal);

        var besin = Assert.Single(sonuc);
        Assert.True(besin.ValuesPerPortion);
        Assert.Equal("Baklava (2 dilim)", besin.Name);
        Assert.Equal("2 dilim", besin.PortionName);
        Assert.Equal(0m, besin.PortionGrams);
        Assert.Equal(330m, besin.EnergyKcalPer100g);   // porsiyon başına
        Assert.Equal("PlateRank", besin.SourceName);
        Assert.Equal("0b6f3c2e-1d1a-4c55-9a0e-3f2a1b9c8d7e", besin.ExternalId);
        Assert.Equal(new DateTime(2026, 10, 21, 13, 58, 35, DateTimeKind.Utc), besin.RetrievedAt);

        // Anahtar başlıkta gider, adreste yok; lisansı uygun olmayan satırlar sorguda istenmez.
        var istek = Assert.Single(handler.Istekler);
        Assert.Equal("deneme-anahtari", istek.Headers.GetValues("apikey").Single());
        var adres = Uri.UnescapeDataString(istek.RequestUri!.ToString());
        Assert.DoesNotContain("deneme-anahtari", adres);
        Assert.Contains("ad=ilike.*baklava*", adres);
        Assert.Contains("kaynak.not.ilike.*mcdonald*", adres);
        Assert.Contains("yayinda=eq.true", adres);
    }

    [Fact]
    public async Task PlateRank_porsiyon_grami_varsa_degerler_100_grama_cevrilir()
    {
        var handler = new SahteHandler(HttpStatusCode.OK, """
            [{"id":"5d0c2b1a-9e8f-4a7b-8c6d-1e2f3a4b5c6d","ad":"Waffle (sade, 2 adet)","mutfak":"us","kcal":560,"protein":13,
              "karbonhidrat":66,"yag":28,"porsiyon_g":150,"kaynak":"USDA FDC:2708325","updated_at":"2026-10-21T13:58:35+00:00"}]
            """);
        var adapter = PlateRank(handler, anahtar: "deneme-anahtari");

        var besin = Assert.Single(await adapter.SearchAsync("waffle", 20, Iptal));

        Assert.False(besin.ValuesPerPortion);
        Assert.Equal(150m, besin.PortionGrams);
        Assert.Equal(373.3m, besin.EnergyKcalPer100g);   // 560 × 100 / 150
        Assert.Equal(8.7m, besin.ProteinPer100g);
        Assert.Equal(44m, besin.CarbsPer100g);
        Assert.Equal(18.7m, besin.FatPer100g);
    }

    [Fact]
    public async Task PlateRank_anahtar_yoksa_ya_da_sunucu_hata_verirse_kullanilamaz_sayilir()
    {
        var anahtarsiz = PlateRank(new SahteHandler(HttpStatusCode.OK, "[]"), anahtar: null);
        await Assert.ThrowsAsync<ExternalSourceUnavailableException>(() => anahtarsiz.SearchAsync("kısır", 20, Iptal));

        var hatali = PlateRank(new SahteHandler(HttpStatusCode.InternalServerError, "{}"), anahtar: "deneme-anahtari");
        await Assert.ThrowsAsync<ExternalSourceUnavailableException>(() => hatali.SearchAsync("kısır", 20, Iptal));

        // Kimlik GUID değilse istek atılmaz
        Assert.Null(await hatali.GetByExternalIdAsync("173904", Iptal));
    }

    [Fact]
    public async Task DeepL_metinleri_tek_istekte_cevirir_ve_ikinci_kez_sormaz()
    {
        var handler = new SahteHandler(HttpStatusCode.OK, """
            {"translations":[{"detected_source_language":"EN","text":"Mercimek, pişmiş"},
                             {"detected_source_language":"EN","text":"Yulaf ezmesi"}]}
            """);
        using var bellek = new MemoryCache(new MemoryCacheOptions());
        var adapter = DeepL(handler, bellek, anahtar: "deneme:fx");

        var ilk = await adapter.TranslateAsync(["Lentils, cooked", "Oats, rolled"], "en", "tr", Iptal);
        var ikinci = await adapter.TranslateAsync(["Oats, rolled"], "en", "tr", Iptal);

        Assert.Equal(new[] { "Mercimek, pişmiş", "Yulaf ezmesi" }, ilk);
        Assert.Equal(new[] { "Yulaf ezmesi" }, ikinci);
        var istek = Assert.Single(handler.Istekler);   // ikinci çağrı bellekten
        Assert.Equal("DeepL-Auth-Key deneme:fx", istek.Headers.GetValues("Authorization").Single());
        Assert.Contains("\"target_lang\":\"TR\"", handler.Govdeler.Single());
        Assert.Contains("\"source_lang\":\"EN\"", handler.Govdeler.Single());
    }

    [Fact]
    public async Task DeepL_kota_dolunca_ya_da_anahtar_yoksa_metinler_aynen_doner()
    {
        using var bellek = new MemoryCache(new MemoryCacheOptions());

        // 456: aylık karakter kotası doldu
        var kotaDolu = DeepL(new SahteHandler((HttpStatusCode)456, """{"message":"Quota exceeded"}"""), bellek, anahtar: "deneme:fx");
        Assert.Equal(new[] { "mercimek" }, await kotaDolu.TranslateAsync(["mercimek"], "tr", "en", Iptal));

        var anahtarsizHandler = new SahteHandler(HttpStatusCode.OK, "{}");
        var anahtarsiz = DeepL(anahtarsizHandler, bellek, anahtar: null);
        Assert.Equal(new[] { "mercimek" }, await anahtarsiz.TranslateAsync(["mercimek"], "tr", "en", Iptal));
        Assert.Empty(anahtarsizHandler.Istekler);
    }

    [Fact]
    public void DeepL_adresi_anahtara_gore_secilir()
    {
        Assert.Equal("https://api-free.deepl.com/v2/", new DeepLOptions { ApiKey = "abc:fx" }.ResolveBaseUrl());
        Assert.Equal("https://api.deepl.com/v2/", new DeepLOptions { ApiKey = "abc" }.ResolveBaseUrl());
        Assert.Equal("https://ornek.test/", new DeepLOptions { ApiKey = "abc", BaseUrl = "https://ornek.test/" }.ResolveBaseUrl());
    }

    private static PlateRankAdapter PlateRank(SahteHandler handler, string? anahtar)
    {
        var options = Options.Create(new PlateRankOptions { ApiKey = anahtar, BaseUrl = PlateRankAdresi });
        var client = new PlateRankClient(new HttpClient(handler) { BaseAddress = new Uri(PlateRankAdresi) }, options);
        return new PlateRankAdapter(
            client, new ApiQuotaCounter(10, TimeProvider.System), TimeProvider.System, NullLogger<PlateRankAdapter>.Instance);
    }

    private static DeepLTranslationAdapter DeepL(SahteHandler handler, IMemoryCache bellek, string? anahtar)
    {
        var options = Options.Create(new DeepLOptions { ApiKey = anahtar });
        var http = new HttpClient(handler) { BaseAddress = new Uri(options.Value.ResolveBaseUrl()) };
        return new DeepLTranslationAdapter(http, options, bellek, NullLogger<DeepLTranslationAdapter>.Instance);
    }

    /// <summary>Her isteğe aynı yanıtı verir; gelen istekleri ve gövdelerini saklar.</summary>
    private sealed class SahteHandler(HttpStatusCode durum, string yanit) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Istekler { get; } = [];

        public List<string> Govdeler { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Istekler.Add(request);
            Govdeler.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(durum) { Content = new StringContent(yanit, Encoding.UTF8, "application/json") };
        }
    }
}
