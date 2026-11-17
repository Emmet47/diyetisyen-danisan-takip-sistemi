using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static Ddts.IntegrationTests.ModulYardimcisi;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Ogun;

/// <summary>
/// Öğün günlüğü, fotoğraf ve ilerleme raporu (G, K modülleri, Şekil 5.5 ve 5.6): bugünkü öğünler (FR-E5),
/// öğün kaydı (FR-G1, BR-11), fotoğraf türü/boyutu (IT-09, BR-10), fotoğraf bağlantısında sahiplik (IT-10),
/// planlanan–gerçekleşen (FR-G3), panel ve PDF (KT-03, FR-K3). Fotoğraflar testte bellekte tutulur.
/// Bu sınıf toplam 1 giriş yapar.
/// </summary>
public sealed class OgunTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52];
    private static readonly byte[] Gif = "GIF89a"u8.ToArray().Concat(new byte[64]).ToArray();

    [Fact]
    public async Task Danisan_bugunku_ogunu_kaydeder_fotograf_ekler_diyetisyen_gorur_ve_rapor_uretir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisan = await DanisanHesabiAcAsync(factory, diyetisyen.Http);
        var besin = await BesinEkleAsync(diyetisyen.Http, "Kahvaltı tabağı", 100m, 200m, 10m, 20m, 8m);
        var planId = await YayimlanmisPlanKurAsync(diyetisyen.Http, danisan.Id, besin);
        var bugun = Bugun();

        // Bugün ekranı: günlük planın kahvaltısı, henüz kaydı yok
        var bugunEkrani = (await danisan.Http.GetFromJsonAsync<BugunYaniti>("/api/me/meals/today", Iptal))!;
        Assert.Equal(planId, bugunEkrani.PlanId);
        var ogun = Assert.Single(bugunEkrani.Meals);
        Assert.Null(ogun.Log);
        var ogunId = ogun.Meal.Id;

        // İleri bir gün için kayıt yapılamaz (400); bugün için yapılır (201); ikinci kez 409 (BR-11)
        var ileri = await danisan.Http.PostAsJsonAsync("/api/me/meal-logs", KayitFormu(ogunId, bugun.AddDays(1), "Eaten"), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, ileri.StatusCode);

        var kayit = await danisan.Http.PostAsJsonAsync("/api/me/meal-logs", KayitFormu(ogunId, bugun, "Partial", "Yarısını yedim"), Iptal);
        Assert.Equal(HttpStatusCode.Created, kayit.StatusCode);
        var kayitId = (await kayit.Content.ReadFromJsonAsync<KimlikYaniti>(Iptal))!.Id;

        var tekrar = await danisan.Http.PostAsJsonAsync("/api/me/meal-logs", KayitFormu(ogunId, bugun, "Eaten"), Iptal);
        Assert.Equal(HttpStatusCode.Conflict, tekrar.StatusCode);

        var guncelleme = await danisan.Http.PutAsJsonAsync($"/api/me/meal-logs/{kayitId}", new { status = "Eaten", note = "Hepsini yedim" }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, guncelleme.StatusCode);

        // Fotoğraf: GIF reddedilir (IT-09), 5 MB'tan büyük dosya reddedilir, PNG kabul edilir, ikincisi 409
        var gif = await FotografYukleAsync(danisan.Http, kayitId, Gif, "ogle.gif");
        Assert.Equal(HttpStatusCode.BadRequest, gif.StatusCode);
        var gifHatasi = (await gif.Content.ReadFromJsonAsync<ProblemYaniti>(Iptal))!;
        Assert.Equal("Geçersiz dosya", gifHatasi.Title);
        Assert.Equal("Sadece JPEG/PNG, en fazla 5 MB yükleyebilirsin.", gifHatasi.Detail);

        var buyuk = await FotografYukleAsync(danisan.Http, kayitId, Png.Concat(new byte[5 * 1024 * 1024]).ToArray(), "buyuk.png");
        Assert.Equal(HttpStatusCode.BadRequest, buyuk.StatusCode);

        // Uzantı .jpg olsa da içerik PNG: türe dosya imzasından bakılır
        var png = await FotografYukleAsync(danisan.Http, kayitId, Png, "ogle.jpg");
        Assert.Equal(HttpStatusCode.OK, png.StatusCode);
        var anahtar = Assert.Single(factory.Photos.Keys, k => k.StartsWith(danisan.Id.ToString("N"), StringComparison.Ordinal));
        Assert.EndsWith(".png", anahtar);

        var ikinci = await FotografYukleAsync(danisan.Http, kayitId, Png, "ikinci.png");
        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);

        // 10 dakikalık imzalı bağlantı: danışan ve kendi diyetisyeni alır, başka diyetisyen alamaz (IT-10)
        var danisanBaglantisi = (await danisan.Http.GetFromJsonAsync<FotografBaglantisi>($"/api/meal-logs/{kayitId}/photo-url", Iptal))!;
        Assert.Contains("X-Amz-Expires=600", danisanBaglantisi.Url);
        Assert.InRange(danisanBaglantisi.ExpiresAt, DateTime.UtcNow.AddMinutes(9), DateTime.UtcNow.AddMinutes(11));
        Assert.Equal(HttpStatusCode.OK, (await diyetisyen.Http.GetAsync($"/api/meal-logs/{kayitId}/photo-url", Iptal)).StatusCode);

        var diger = await DiyetisyenAcAsync(factory);
        Assert.Equal(HttpStatusCode.NotFound, (await diger.Http.GetAsync($"/api/meal-logs/{kayitId}/photo-url", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diger.Http.GetAsync($"/api/clients/{danisan.Id}/meal-logs", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diger.Http.GetAsync($"/api/clients/{danisan.Id}/progress", Iptal)).StatusCode);

        // Planlanan–gerçekleşen (FR-G3): bugün kahvaltı "Yedim"
        var karsilastirma = (await diyetisyen.Http.GetFromJsonAsync<Karsilastirma>(
            $"/api/clients/{danisan.Id}/meal-logs?from={bugun:yyyy-MM-dd}&to={bugun:yyyy-MM-dd}", Iptal))!;
        var gun = Assert.Single(karsilastirma.Days);
        var planlanan = Assert.Single(gun.Meals);
        Assert.Equal(ogunId, planlanan.MealId);
        Assert.Equal("Eaten", planlanan.Log!.Status);
        Assert.True(planlanan.Log.HasPhoto);

        // Panel (KT-03, FR-K3): bugün tek planlı öğün, "Yedim" → uyum %100, sıklık 1/1 gün
        var olcum = await diyetisyen.Http.PostAsJsonAsync($"/api/clients/{danisan.Id}/measurements",
            new { measuredOn = bugun.ToString("yyyy-MM-dd"), weightKg = 70.0m, heightCm = 165.0m, waistCm = 80.0m }, Iptal);
        Assert.Equal(HttpStatusCode.Created, olcum.StatusCode);

        var panel = (await diyetisyen.Http.GetFromJsonAsync<Panel>(
            $"/api/clients/{danisan.Id}/progress?from={bugun:yyyy-MM-dd}&to={bugun:yyyy-MM-dd}", Iptal))!;
        Assert.Equal(1, panel.PlannedMeals);
        Assert.Equal(1, panel.EatenMeals);
        Assert.Equal(100m, panel.AdherencePercent);
        Assert.Equal(1, panel.LoggedDays);
        Assert.Equal(100m, panel.LogFrequencyPercent);
        Assert.Equal(5m, panel.RemainingKg);   // 70 − hedef 65
        Assert.Single(panel.Measurements);

        // PDF raporu (FR-K2)
        var pdf = await diyetisyen.Http.GetAsync($"/api/clients/{danisan.Id}/report.pdf", Iptal);
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        var icerik = await pdf.Content.ReadAsByteArrayAsync(Iptal);
        Assert.Equal("%PDF"u8.ToArray(), icerik.Take(4).ToArray());

        // Danışan rapor uçlarını kullanamaz (403); kalıcı silmede fotoğraf depodan da silinir (belge 2.5 madde 4)
        Assert.Equal(HttpStatusCode.Forbidden, (await danisan.Http.GetAsync($"/api/clients/{danisan.Id}/progress", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await diyetisyen.Http.DeleteAsync($"/api/clients/{danisan.Id}", Iptal)).StatusCode);
        Assert.DoesNotContain(factory.Photos.Keys, k => k == anahtar);
    }

    private static object KayitFormu(Guid ogunId, DateOnly gun, string durum, string? not = null) =>
        new { mealId = ogunId, logDate = gun.ToString("yyyy-MM-dd"), status = durum, note = not };

    private static async Task<HttpResponseMessage> FotografYukleAsync(HttpClient http, Guid kayitId, byte[] dosya, string dosyaAdi)
    {
        using var form = new MultipartFormDataContent();
        var parca = new ByteArrayContent(dosya);
        parca.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(parca, "file", dosyaAdi);
        return await http.PostAsync($"/api/me/meal-logs/{kayitId}/photo", form, Iptal);
    }

    private sealed record OgunBilgisi(Guid Id, int DayNo, string Name);

    private sealed record KayitBilgisi(Guid Id, Guid MealId, string Status, string? Note, bool HasPhoto);

    private sealed record BugunkuOgun(OgunBilgisi Meal, KayitBilgisi? Log);

    private sealed record BugunYaniti(DateOnly Date, Guid? PlanId, List<BugunkuOgun> Meals);

    private sealed record FotografBaglantisi(string Url, DateTime ExpiresAt);

    private sealed record KarsilastirmaOgunu(Guid MealId, string Name, KayitBilgisi? Log);

    private sealed record KarsilastirmaGunu(DateOnly Date, List<KarsilastirmaOgunu> Meals);

    private sealed record Karsilastirma(DateOnly From, DateOnly To, List<KarsilastirmaGunu> Days);

    private sealed record Panel(int PlannedMeals, int EatenMeals, int PartialMeals, decimal? AdherencePercent,
        int LoggedDays, int DayCount, decimal LogFrequencyPercent, decimal? RemainingKg, List<object> Measurements);
}
