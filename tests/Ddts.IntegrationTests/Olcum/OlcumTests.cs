using System.Net;
using System.Net.Http.Json;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Olcum;

/// <summary>
/// Ölçüm ekleme, geçmiş, düzeltme ve silme (C modülü, Şekil 5.6). IT-01 (başka diyetisyenin danışanı → 404),
/// IT-02 (rol → 403), KT-07 (danışan kendi geçmişini görür), DB-20 (aynı gün ikinci ölçüm → 409).
/// Ölçüm değerleri sahtedir. Not: bu sınıftaki testler toplam 1 giriş yapar (giriş sınırı dakikada 5).
/// </summary>
public sealed class OlcumTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Gecmis_eskiden_yeniye_farklarla_ve_hedefe_kalan_kiloyla_doner_danisan_kendisininkini_gorur()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var eposta = YeniEposta("danisan");
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, eposta);   // hedef kilo 65.0
        var yol = $"/api/clients/{danisanId}/measurements";

        // Ölçüm henüz yok: liste boş, hedefe kalan kilo hesaplanamaz
        var bos = await diyetisyen.Http.GetFromJsonAsync<OlcumGecmisi>(yol, Iptal);
        Assert.Equal(65.0m, bos!.TargetWeightKg);
        Assert.Null(bos.RemainingKg);
        Assert.Empty(bos.Items);

        // Sonraki gün önce girilse de liste tarih sırasıyla döner (FR-C2)
        var ikinci = await diyetisyen.Http.PostAsJsonAsync(yol, OlcumFormu("2026-10-08", 71.6m, 83.0m, kalca: 99.0m), Iptal);
        Assert.Equal(HttpStatusCode.Created, ikinci.StatusCode);
        var ikinciId = (await ikinci.Content.ReadFromJsonAsync<OlcumEklemeYaniti>(Iptal))!.Id;
        Assert.EndsWith($"{yol}/{ikinciId}", ikinci.Headers.Location!.ToString());

        var birinci = await diyetisyen.Http.PostAsJsonAsync(yol, OlcumFormu("2026-10-01", 72.4m, 84.5m), Iptal);
        Assert.Equal(HttpStatusCode.Created, birinci.StatusCode);

        var gecmis = (await diyetisyen.Http.GetFromJsonAsync<OlcumGecmisi>(yol, Iptal))!;

        Assert.Equal(new[] { new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 8) }, gecmis.Items.Select(o => o.MeasuredOn));
        Assert.Null(gecmis.Items[0].WeightChangeKg);                 // ilk ölçümde fark yok
        Assert.Equal(-0.8m, gecmis.Items[1].WeightChangeKg);         // 72,4 → 71,6 kg (UT-13 örneği)
        Assert.Equal(-1.5m, gecmis.Items[1].WaistChangeCm);
        Assert.Null(gecmis.Items[1].HipChangeCm);                    // ilk ölçümde kalça girilmedi
        Assert.Equal(ikinciId, gecmis.Items[1].Id);
        Assert.Equal(65.0m, gecmis.TargetWeightKg);
        Assert.Equal(6.6m, gecmis.RemainingKg);                      // son kilo 71,6 − hedef 65 (FR-C3)

        // Danışan kendi geçmişini mobil uygulamadan görür (FR-C4) ama ölçüm ekleyemez (IT-02)
        await DavetiKabulEtAsync(factory, eposta);
        using var danisan = await GirisYapmisIstemciAsync(factory, eposta);

        var kendi = (await danisan.GetFromJsonAsync<OlcumGecmisi>("/api/me/measurements", Iptal))!;
        Assert.Equal(gecmis.Items.Select(o => o.Id), kendi.Items.Select(o => o.Id));
        Assert.Equal(6.6m, kendi.RemainingKg);

        var danisanEklerse = await danisan.PostAsJsonAsync(yol, OlcumFormu("2026-10-15", 71.0m, 82.0m), Iptal);
        Assert.Equal(HttpStatusCode.Forbidden, danisanEklerse.StatusCode);

        // Diyetisyen danışan ucunu kullanamaz
        var diyetisyenKendisi = await diyetisyen.Http.GetAsync("/api/me/measurements", Iptal);
        Assert.Equal(HttpStatusCode.Forbidden, diyetisyenKendisi.StatusCode);
    }

    [Fact]
    public async Task Ayni_gune_ikinci_olcum_girilemez_olcum_duzeltilir_ve_silinir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
        var yol = $"/api/clients/{danisanId}/measurements";

        var ilk = await diyetisyen.Http.PostAsJsonAsync(yol, OlcumFormu("2026-11-01", 72.4m, 84.0m), Iptal);
        var ilkId = (await ilk.Content.ReadFromJsonAsync<OlcumEklemeYaniti>(Iptal))!.Id;
        var sonraki = await diyetisyen.Http.PostAsJsonAsync(yol, OlcumFormu("2026-11-08", 71.9m, 83.5m), Iptal);
        var sonrakiId = (await sonraki.Content.ReadFromJsonAsync<OlcumEklemeYaniti>(Iptal))!.Id;

        // Aynı gün ikinci ölçüm 409 (FR-C1, Senaryo 4 adım 1a)
        var ayniGun = await diyetisyen.Http.PostAsJsonAsync(yol, OlcumFormu("2026-11-01", 72.0m, 83.0m), Iptal);
        Assert.Equal(HttpStatusCode.Conflict, ayniGun.StatusCode);
        var problem = await ayniGun.Content.ReadFromJsonAsync<ProblemYaniti>(Iptal);
        Assert.Contains("zaten bir ölçüm var", problem!.Detail);

        // Düzeltmede de başka ölçümün gününe taşınamaz
        var cakisanDuzeltme = await diyetisyen.Http.PutAsJsonAsync($"{yol}/{sonrakiId}", OlcumFormu("2026-11-01", 71.9m, 83.5m), Iptal);
        Assert.Equal(HttpStatusCode.Conflict, cakisanDuzeltme.StatusCode);

        // Eksik ya da mantıksız değer 400 (DB-21)
        var belsiz = await diyetisyen.Http.PostAsJsonAsync(
            yol, new { measuredOn = "2026-11-15", weightKg = 71.5m, heightCm = 168.0m }, Iptal);
        var asiriKilo = await diyetisyen.Http.PostAsJsonAsync(yol, OlcumFormu("2026-11-15", 500m, 83.0m), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, belsiz.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, asiriKilo.StatusCode);

        // Düzeltme: değerler ve tarih değişir (aynı gün kalırsa çakışma sayılmaz)
        var duzeltme = await diyetisyen.Http.PutAsJsonAsync($"{yol}/{ilkId}", OlcumFormu("2026-11-01", 72.2m, 84.0m, not: "Tartı yeniden ayarlandı"), Iptal);
        var tarihDuzeltme = await diyetisyen.Http.PutAsJsonAsync($"{yol}/{sonrakiId}", OlcumFormu("2026-11-09", 71.9m, 83.5m), Iptal);
        Assert.Equal(HttpStatusCode.NoContent, duzeltme.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, tarihDuzeltme.StatusCode);

        var gecmis = (await diyetisyen.Http.GetFromJsonAsync<OlcumGecmisi>(yol, Iptal))!;
        Assert.Equal(72.2m, gecmis.Items[0].WeightKg);
        Assert.Equal("Tartı yeniden ayarlandı", gecmis.Items[0].Note);
        Assert.Equal(new DateOnly(2026, 11, 9), gecmis.Items[1].MeasuredOn);
        Assert.Equal(-0.3m, gecmis.Items[1].WeightChangeKg);

        // Silme
        var silme = await diyetisyen.Http.DeleteAsync($"{yol}/{ilkId}", Iptal);
        var tekrarSilme = await diyetisyen.Http.DeleteAsync($"{yol}/{ilkId}", Iptal);
        Assert.Equal(HttpStatusCode.NoContent, silme.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, tekrarSilme.StatusCode);

        gecmis = (await diyetisyen.Http.GetFromJsonAsync<OlcumGecmisi>(yol, Iptal))!;
        Assert.Equal(sonrakiId, Assert.Single(gecmis.Items).Id);
        Assert.Null(gecmis.Items[0].WeightChangeKg);

        // Denetim kaydında işlem var, ölçüm değeri (sağlık verisi) yok (FR-A5)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();
        var kayitlar = await db.AuditLogs
            .Where(a => a.EntityType == "Measurement" && (a.EntityId == ilkId || a.EntityId == sonrakiId))
            .ToListAsync(Iptal);

        Assert.Equal(2, kayitlar.Count(k => k.Action == "MeasurementAdded"));
        Assert.Equal(2, kayitlar.Count(k => k.Action == "MeasurementUpdated"));
        Assert.Single(kayitlar, k => k.Action == "MeasurementDeleted");
        Assert.All(kayitlar, k => Assert.Null(k.Details));
    }

    [Fact]
    public async Task Baska_diyetisyenin_danisaninin_olcumlerine_ulasilamaz()
    {
        var diyetisyenA = await DiyetisyenAcAsync(factory);
        var diyetisyenB = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyenA.Http, YeniEposta("danisan"));
        var yol = $"/api/clients/{danisanId}/measurements";

        var eklenen = await diyetisyenA.Http.PostAsJsonAsync(yol, OlcumFormu("2026-11-01", 72.4m, 84.0m), Iptal);
        var olcumId = (await eklenen.Content.ReadFromJsonAsync<OlcumEklemeYaniti>(Iptal))!.Id;

        // Diyetisyen B, A'nın danışanının adresini yazıyor: hepsi 404, kaydın varlığı belli olmaz (IT-01)
        var liste = await diyetisyenB.Http.GetAsync(yol, Iptal);
        var ekleme = await diyetisyenB.Http.PostAsJsonAsync(yol, OlcumFormu("2026-11-02", 72.0m, 84.0m), Iptal);
        var duzeltme = await diyetisyenB.Http.PutAsJsonAsync($"{yol}/{olcumId}", OlcumFormu("2026-11-01", 60m, 70m), Iptal);
        var silme = await diyetisyenB.Http.DeleteAsync($"{yol}/{olcumId}", Iptal);

        Assert.Equal(HttpStatusCode.NotFound, liste.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ekleme.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, duzeltme.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, silme.StatusCode);

        // B'nin kendi danışanının adresiyle A'nın ölçüm kimliği de bulunmaz
        var bDanisani = await DanisanEkleAsync(diyetisyenB.Http, YeniEposta("danisan"));
        var capraz = await diyetisyenB.Http.DeleteAsync($"/api/clients/{bDanisani}/measurements/{olcumId}", Iptal);
        Assert.Equal(HttpStatusCode.NotFound, capraz.StatusCode);

        // Token olmadan 401
        using var anonim = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonim.GetAsync(yol, Iptal)).StatusCode);

        // A'nın ölçümü değişmedi
        var gecmis = (await diyetisyenA.Http.GetFromJsonAsync<OlcumGecmisi>(yol, Iptal))!;
        Assert.Equal(72.4m, Assert.Single(gecmis.Items).WeightKg);
    }

    /// <summary>Sahte ölçüm formu: boy 168 cm sabit, kalça ve not isteğe bağlı.</summary>
    private static object OlcumFormu(string gun, decimal kilo, decimal bel, decimal? kalca = null, string? not = null) => new
    {
        measuredOn = gun,
        weightKg = kilo,
        heightCm = 168.0m,
        waistCm = bel,
        hipCm = kalca,
        bodyFatPercent = (decimal?)null,
        note = not,
    };

    private sealed record OlcumEklemeYaniti(Guid Id);

    private sealed record OlcumGecmisi(decimal? TargetWeightKg, decimal? RemainingKg, List<OlcumSatiri> Items);

    private sealed record OlcumSatiri(
        Guid Id,
        DateOnly MeasuredOn,
        decimal WeightKg,
        decimal HeightCm,
        decimal WaistCm,
        decimal? HipCm,
        decimal? BodyFatPercent,
        string? Note,
        decimal? WeightChangeKg,
        decimal? WaistChangeCm,
        decimal? HipChangeCm,
        decimal? BodyFatChangePercent);
}
