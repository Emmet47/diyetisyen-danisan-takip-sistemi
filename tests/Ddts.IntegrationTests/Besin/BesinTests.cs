using System.Net;
using System.Net.Http.Json;
using static Ddts.IntegrationTests.ModulYardimcisi;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Besin;

/// <summary>
/// Besin veritabanı (D modülü) ve enerji hesabı (H modülü): yerel besin işlemleri (FR-D2), arama (FR-D1),
/// dış kaynak çalışmazken yerel sonuçlar (IT-07, FR-D4), enerji stratejisi (FR-H1). Testte USDA anahtarı yok:
/// dış kaynak her zaman "kullanılamıyor" durumundadır. Bu sınıf hiç giriş yapmaz (kayıt ucu token döndürür).
/// </summary>
public sealed class BesinTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Yerel_besin_eklenir_aranir_duzenlenir_silinir_ortak_ve_baskasinin_besini_degistirilemez()
    {
        var diyetisyenA = await DiyetisyenAcAsync(factory);
        var diyetisyenB = await DiyetisyenAcAsync(factory);

        var besinId = await BesinEkleAsync(diyetisyenA.Http, "Ev yapımı granola", 40m, 450m, 10m, 60m, 18m);

        var arama = await diyetisyenA.Http.GetFromJsonAsync<AramaSonucu>("/api/foods?q=granola&source=Yerel", Iptal);
        var bulunan = Assert.Single(arama!.Items);
        Assert.Equal(besinId, bulunan.Id);
        Assert.Equal("Yerel", bulunan.SourceName);
        Assert.False(arama.ExternalUnavailable);

        // Başka diyetisyen bu kaydı göremez ve değiştiremez (IDOR, 404)
        var bArama = await diyetisyenB.Http.GetFromJsonAsync<AramaSonucu>("/api/foods?q=granola", Iptal);
        Assert.Empty(bArama!.Items);
        var bDuzenleme = await diyetisyenB.Http.PutAsJsonAsync($"/api/foods/{besinId}", BesinFormu("Değişti"), Iptal);
        Assert.Equal(HttpStatusCode.NotFound, bDuzenleme.StatusCode);

        // Sahibi düzenler; makro toplamı 100 g'ı geçemez (400)
        var duzenleme = await diyetisyenA.Http.PutAsJsonAsync($"/api/foods/{besinId}", BesinFormu("Granola (şekersiz)"), Iptal);
        Assert.Equal(HttpStatusCode.NoContent, duzenleme.StatusCode);
        var hatali = await diyetisyenA.Http.PutAsJsonAsync($"/api/foods/{besinId}", BesinFormu("Hatalı", protein: 70m), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, hatali.StatusCode);

        // Ortak (hazır yüklenen) besin herkese görünür ama düzenlenemez, silinemez (FR-D2, 403)
        var ortak = (await diyetisyenA.Http.GetFromJsonAsync<AramaSonucu>("/api/foods?q=Yulaf", Iptal))!.Items
            .Single(i => i.Name == "Yulaf ezmesi");
        var ortakDuzenleme = await diyetisyenA.Http.PutAsJsonAsync($"/api/foods/{ortak.Id}", BesinFormu("Yulaf"), Iptal);
        var ortakSilme = await diyetisyenA.Http.DeleteAsync($"/api/foods/{ortak.Id}", Iptal);
        Assert.Equal(HttpStatusCode.Forbidden, ortakDuzenleme.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, ortakSilme.StatusCode);

        var silme = await diyetisyenA.Http.DeleteAsync($"/api/foods/{besinId}", Iptal);
        Assert.Equal(HttpStatusCode.NoContent, silme.StatusCode);
        Assert.Empty((await diyetisyenA.Http.GetFromJsonAsync<AramaSonucu>("/api/foods?q=granola", Iptal))!.Items);
    }

    [Fact]
    public async Task Dis_kaynak_kullanilamazsa_arama_yerel_sonuclarla_ve_uyariyla_doner()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);

        // IT-07: 500 değil 200; yerel kayıtlar ve uyarı mesajı (FR-D4, NFR-E1)
        var yanit = await diyetisyen.Http.GetAsync("/api/foods?q=yulaf&external=true", Iptal);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);

        var sonuc = (await yanit.Content.ReadFromJsonAsync<AramaSonucu>(Iptal))!;
        Assert.True(sonuc.ExternalUnavailable);
        Assert.Equal("Dış kaynağa şu an ulaşılamıyor, yerel kayıtlar gösteriliyor.", sonuc.Message);
        Assert.Contains(sonuc.Items, i => i.Name == "Yulaf ezmesi" && i.SourceName == "Yerel");

        // İçe aktarma dış kaynak olmadan yapılamaz: 503, ayrıntı gösterilmez
        var aktarma = await diyetisyen.Http.PostAsJsonAsync("/api/foods/import", new { externalId = "173904" }, Iptal);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, aktarma.StatusCode);

        // Danışan besin arayamaz (rol, 403); token olmadan 401
        using var anonim = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonim.GetAsync("/api/foods?q=yulaf", Iptal)).StatusCode);
    }

    [Fact]
    public async Task Enerji_secilen_formule_gore_son_olcum_ve_yasla_hesaplanir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));   // kadın, 1996-01-01

        // Ölçüm yokken hesap yapılamaz (400)
        var olcumsuz = await diyetisyen.Http.PostAsJsonAsync("/api/energy/calculate", EnerjiFormu(danisanId, "MifflinStJeor"), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, olcumsuz.StatusCode);

        var olcum = await diyetisyen.Http.PostAsJsonAsync(
            $"/api/clients/{danisanId}/measurements",
            new { measuredOn = Bugun().ToString("yyyy-MM-dd"), weightKg = 70.0m, heightCm = 165.0m, waistCm = 80.0m },
            Iptal);
        Assert.Equal(HttpStatusCode.Created, olcum.StatusCode);

        var yanit = await diyetisyen.Http.PostAsJsonAsync("/api/energy/calculate", EnerjiFormu(danisanId, "MifflinStJeor"), Iptal);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var sonuc = (await yanit.Content.ReadFromJsonAsync<EnerjiSonucu>(Iptal))!;

        var yas = sonuc.Input.AgeYears;
        var beklenenBmh = 10m * 70m + 6.25m * 165m - 5m * yas - 161m;
        Assert.Equal("Female", sonuc.Input.Sex);
        Assert.Equal(beklenenBmh, sonuc.BmrKcal);
        Assert.Equal(Math.Round(beklenenBmh * 1.55m, 2, MidpointRounding.AwayFromZero), sonuc.DailyNeedKcal);
        Assert.Equal(sonuc.DailyNeedKcal - 500m, sonuc.TargetKcal);
        Assert.Equal("Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.", sonuc.Disclaimer);

        // Diğer strateji farklı sonuç verir; bilinmeyen formül 400
        var hb = (await (await diyetisyen.Http.PostAsJsonAsync("/api/energy/calculate", EnerjiFormu(danisanId, "HarrisBenedict"), Iptal))
            .Content.ReadFromJsonAsync<EnerjiSonucu>(Iptal))!;
        Assert.Equal("HarrisBenedict", hb.Formula);
        Assert.NotEqual(sonuc.BmrKcal, hb.BmrKcal);
        var bilinmeyen = await diyetisyen.Http.PostAsJsonAsync("/api/energy/calculate", EnerjiFormu(danisanId, "Katch"), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, bilinmeyen.StatusCode);

        // Başka diyetisyenin danışanı için hesap yapılamaz (404)
        var diger = await DiyetisyenAcAsync(factory);
        var izinsiz = await diger.Http.PostAsJsonAsync("/api/energy/calculate", EnerjiFormu(danisanId, "MifflinStJeor"), Iptal);
        Assert.Equal(HttpStatusCode.NotFound, izinsiz.StatusCode);
    }

    private static object BesinFormu(string ad, decimal protein = 10m) => new
    {
        name = ad,
        portionName = "1 kase",
        portionGrams = 40m,
        energyKcalPer100g = 430m,
        proteinPer100g = protein,
        carbsPer100g = 60m,
        fatPer100g = 15m,
    };

    private static object EnerjiFormu(Guid danisanId, string formul) =>
        new { clientId = danisanId, formula = formul, activityFactor = 1.55m, adjustmentKcal = -500 };

    private sealed record AramaOgesi(Guid? Id, string Name, string PortionName, decimal PortionGrams, decimal EnergyKcalPer100g,
        string SourceName, string? ExternalId, DateTime? RetrievedAt);

    private sealed record AramaSonucu(List<AramaOgesi> Items, bool ExternalUnavailable, string? Message);

    private sealed record EnerjiGirdisi(string Sex, int AgeYears, decimal WeightKg, decimal HeightCm);

    private sealed record EnerjiSonucu(string Formula, EnerjiGirdisi Input, decimal BmrKcal, decimal DailyNeedKcal, decimal TargetKcal, string Disclaimer);
}
