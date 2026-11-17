using System.Net;
using System.Net.Http.Json;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ddts.IntegrationTests.ModulYardimcisi;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Plan;

/// <summary>
/// Beslenme planı ve şablonlar (E, F modülleri): Builder ile plan (FR-E1, BR-05), Composite toplamları (FR-H2),
/// yayımlama ve bildirim (IT-08, BR-03), şablon kopyasının bağımsızlığı (IT-06, BR-07), başka diyetisyen (IT-01).
/// Bu sınıf toplam 1 giriş yapar.
/// </summary>
public sealed class PlanTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Plan_kurulur_alerjen_yayimlanamaz_yayimlaninca_eskisi_arsivlenir_ve_danisana_bildirim_gider()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisan = await DanisanHesabiAcAsync(factory, diyetisyen.Http);
        // 1 porsiyon = 50 g; 100 g'da 150 kcal, 12 g protein, 1 g karbonhidrat, 10 g yağ
        var yumurta = await BesinEkleAsync(diyetisyen.Http, "Deneme yumurtası", 50m, 150m, 12m, 1m, 10m);
        var peynir = await BesinEkleAsync(diyetisyen.Http, "Deneme peyniri", 30m, 260m, 17m, 2m, 21m);
        var fistik = await BesinEkleAsync(diyetisyen.Http, "Deneme fıstığı", 20m, 570m, 25m, 16m, 49m);

        var kisit = await diyetisyen.Http.PutAsJsonAsync($"/api/clients/{danisan.Id}/restrictions", new
        {
            restrictions = new[] { new { foodItemId = fistik, type = "Allergy" }, new { foodItemId = peynir, type = "Dislike" } },
        }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, kisit.StatusCode);

        // Alerji listesindeki besin plana giremez: 400 ve hata listesi (BR-05)
        var alerjen = await diyetisyen.Http.PostAsJsonAsync("/api/plans", GunlukPlanFormu(danisan.Id, "Alerjen", fistik, 1m), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, alerjen.StatusCode);
        Assert.Contains("alerji listesinde", await alerjen.Content.ReadAsStringAsync(Iptal));

        // 2 yumurta (100 g) + alternatif peynir: toplam yalnızca yumurta (FR-E2); sevmediği besin uyarı verir
        var olusan = await diyetisyen.Http.PostAsJsonAsync("/api/plans", GunlukPlanFormu(danisan.Id, "İlk plan", yumurta, 2m, peynir), Iptal);
        Assert.Equal(HttpStatusCode.Created, olusan.StatusCode);
        var ilk = (await olusan.Content.ReadFromJsonAsync<PlanSonucu>(Iptal))!;
        Assert.Equal("Draft", ilk.Status);
        Assert.Equal(150m, ilk.Totals.EnergyKcal);
        Assert.Equal(12m, ilk.Totals.ProteinG);
        Assert.Contains("Deneme peyniri: danışan bu besini sevmiyor.", ilk.Warnings);

        var ozet = (await diyetisyen.Http.GetFromJsonAsync<PlanOzeti>($"/api/plans/{ilk.PlanId}", Iptal))!;
        Assert.Equal(2, Assert.Single(ozet.Meals).Items.Count);
        Assert.Equal(150m, Assert.Single(ozet.Days).Totals.EnergyKcal);

        // Taslak düzenlenir; besin planda kullanıldığı için silinemez (FR-D2, 409)
        var duzenleme = await diyetisyen.Http.PutAsJsonAsync($"/api/plans/{ilk.PlanId}", GunlukPlanFormu(danisan.Id, "İlk plan", yumurta, 3m), Iptal);
        Assert.Equal(HttpStatusCode.OK, duzenleme.StatusCode);
        Assert.Equal(225m, (await duzenleme.Content.ReadFromJsonAsync<PlanSonucu>(Iptal))!.Totals.EnergyKcal);
        Assert.Equal(HttpStatusCode.Conflict, (await diyetisyen.Http.DeleteAsync($"/api/foods/{yumurta}", Iptal)).StatusCode);

        // Yayımla: danışan planını görür, "Yeni planın hazır" bildirimi gelir (IT-08)
        var yayim = await diyetisyen.Http.PostAsync($"/api/plans/{ilk.PlanId}/publish", null, Iptal);
        Assert.Equal(HttpStatusCode.OK, yayim.StatusCode);
        var benimPlanim = (await danisan.Http.GetFromJsonAsync<PlanOzeti>("/api/me/plan", Iptal))!;
        Assert.Equal(ilk.PlanId, benimPlanim.Id);

        var bildirimler = (await danisan.Http.GetFromJsonAsync<BildirimSayfasi>("/api/notifications", Iptal))!;
        Assert.Equal("Yeni planın hazır", Assert.Single(bildirimler.Items, b => b.Type == "PlanPublished").Title);

        // Yayımlanmış plan düzenlenemez, silinemez (409, BR-04)
        Assert.Equal(HttpStatusCode.Conflict,
            (await diyetisyen.Http.PutAsJsonAsync($"/api/plans/{ilk.PlanId}", GunlukPlanFormu(danisan.Id, "X", yumurta, 1m), Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await diyetisyen.Http.DeleteAsync($"/api/plans/{ilk.PlanId}", Iptal)).StatusCode);

        // Yeni plan yayımlanınca eskisi arşivlenir: danışanın tek yayımlanmış planı olur (BR-03)
        var ikinci = await YayimlanmisPlanKurAsync(diyetisyen.Http, danisan.Id, yumurta);
        var liste = (await diyetisyen.Http.GetFromJsonAsync<List<PlanSatiri>>($"/api/plans?clientId={danisan.Id}", Iptal))!;
        Assert.Equal("Archived", liste.Single(p => p.Id == ilk.PlanId).Status);
        Assert.Equal("Published", liste.Single(p => p.Id == ikinci).Status);
        Assert.Equal(ikinci, (await danisan.Http.GetFromJsonAsync<PlanOzeti>("/api/me/plan", Iptal))!.Id);

        // Danışan plan uçlarını kullanamaz (403)
        Assert.Equal(HttpStatusCode.Forbidden, (await danisan.Http.GetAsync("/api/plans", Iptal)).StatusCode);
    }

    [Fact]
    public async Task Sablon_danisana_bagimsiz_kopyalanir_kopya_duzenlenince_sablon_degismez()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
        var yumurta = await BesinEkleAsync(diyetisyen.Http, "Şablon yumurtası", 50m, 150m, 12m, 1m, 10m);
        var fistik = await BesinEkleAsync(diyetisyen.Http, "Şablon fıstığı", 20m, 570m, 25m, 16m, 49m);
        await diyetisyen.Http.PutAsJsonAsync($"/api/clients/{danisanId}/restrictions",
            new { restrictions = new[] { new { foodItemId = fistik, type = "Allergy" } } }, Iptal);

        var sablonYaniti = await diyetisyen.Http.PostAsJsonAsync("/api/templates", new
        {
            name = "Akdeniz kahvaltısı",
            description = "Deneme şablonu",
            planType = "Daily",
            meals = new[]
            {
                new
                {
                    dayNo = 1,
                    name = "Kahvaltı",
                    time = "08:00:00",
                    items = new[] { new { foodItemId = yumurta, portionCount = 2m }, new { foodItemId = fistik, portionCount = 1m } },
                },
            },
        }, Iptal);
        Assert.Equal(HttpStatusCode.Created, sablonYaniti.StatusCode);
        var sablonId = (await sablonYaniti.Content.ReadFromJsonAsync<KimlikYaniti>(Iptal))!.Id;

        // Kopya taslak plandır; danışanın alerjik olduğu besin uyarı olarak döner (FR-F2, örnek 4)
        var kopyaYaniti = await diyetisyen.Http.PostAsJsonAsync($"/api/templates/{sablonId}/copy-to-client",
            new { clientId = danisanId, title = "Kasım planı" }, Iptal);
        Assert.Equal(HttpStatusCode.Created, kopyaYaniti.StatusCode);
        var kopya = (await kopyaYaniti.Content.ReadFromJsonAsync<PlanSonucu>(Iptal))!;
        Assert.Equal("Draft", kopya.Status);
        Assert.Equal(sablonId, kopya.SourceTemplateId);
        Assert.Contains("Şablon fıstığı: danışanın alerji listesinde, yayımlamadan önce değiştirmelisin.", kopya.Warnings);

        // Alerjen besinli kopya yayımlanamaz (400)
        Assert.Equal(HttpStatusCode.BadRequest, (await diyetisyen.Http.PostAsync($"/api/plans/{kopya.PlanId}/publish", null, Iptal)).StatusCode);

        // Kopyada porsiyon değişir, fıstık çıkar (IT-06): şablon aynı kalır
        var duzenleme = await diyetisyen.Http.PutAsJsonAsync($"/api/plans/{kopya.PlanId}", GunlukPlanFormu(danisanId, "Kasım planı", yumurta, 1m), Iptal);
        Assert.Equal(HttpStatusCode.OK, duzenleme.StatusCode);

        var sablon = (await diyetisyen.Http.GetFromJsonAsync<SablonOzeti>($"/api/templates/{sablonId}", Iptal))!;
        var sablonOgunu = Assert.Single(sablon.Meals);
        Assert.Equal(2, sablonOgunu.Items.Count);
        Assert.Equal(2m, sablonOgunu.Items.Single(i => i.FoodItemId == yumurta).PortionCount);

        var plan = (await diyetisyen.Http.GetFromJsonAsync<PlanOzeti>($"/api/plans/{kopya.PlanId}", Iptal))!;
        var planOgunu = Assert.Single(plan.Meals);
        Assert.Equal(1m, Assert.Single(planOgunu.Items).PortionCount);
        Assert.NotEqual(sablonOgunu.Id, planOgunu.Id);

        // Şablon silinince kopyalanmış plan kalır, yalnızca kaynak bağlantısı boşalır
        Assert.Equal(HttpStatusCode.NoContent, (await diyetisyen.Http.DeleteAsync($"/api/templates/{sablonId}", Iptal)).StatusCode);
        Assert.Null((await diyetisyen.Http.GetFromJsonAsync<PlanOzeti>($"/api/plans/{kopya.PlanId}", Iptal))!.SourceTemplateId);
    }

    [Fact]
    public async Task Baska_diyetisyen_plana_ve_sablona_ulasamaz_abonelik_bitince_yeni_plan_kurulamaz()
    {
        var diyetisyenA = await DiyetisyenAcAsync(factory);
        var diyetisyenB = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyenA.Http, YeniEposta("danisan"));
        var besin = await BesinEkleAsync(diyetisyenA.Http, "A'nın besini", 100m, 100m, 5m, 10m, 2m);
        var planId = await TaslakPlanKurAsync(diyetisyenA.Http, danisanId, besin);

        // IT-01: B, A'nın kayıtlarına ulaşamaz; varlıkları bile belli olmaz
        Assert.Equal(HttpStatusCode.NotFound, (await diyetisyenB.Http.GetAsync($"/api/plans/{planId}", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diyetisyenB.Http.PostAsync($"/api/plans/{planId}/publish", null, Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diyetisyenB.Http.DeleteAsync($"/api/plans/{planId}", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diyetisyenB.Http.GetAsync($"/api/plans?clientId={danisanId}", Iptal)).StatusCode);

        // B kendi danışanına A'nın besiniyle plan kuramaz (besin görünmez → 400)
        var bDanisani = await DanisanEkleAsync(diyetisyenB.Http, YeniEposta("danisan"));
        var izinsizBesin = await diyetisyenB.Http.PostAsJsonAsync("/api/plans", GunlukPlanFormu(bDanisani, "Plan", besin, 1m), Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, izinsizBesin.StatusCode);

        // BR-01: abonelik süresi dolmuşsa yeni plan kurulamaz (409)
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();
            await db.Database.ExecuteSqlAsync(
                $"UPDATE subscriptions SET start_date = CURRENT_DATE - 40, end_date = CURRENT_DATE - 10 WHERE status = 'Active' AND dietitian_id = (SELECT dietitian_id FROM clients WHERE id = {danisanId})",
                Iptal);
        }

        var suresiDolmus = await diyetisyenA.Http.PostAsJsonAsync("/api/plans", GunlukPlanFormu(danisanId, "Yeni", besin, 1m), Iptal);
        Assert.Equal(HttpStatusCode.Conflict, suresiDolmus.StatusCode);
    }

    private sealed record PlanSatiri(Guid Id, Guid ClientId, string Title, string PlanType, string Status);

    private sealed record SatirOzeti(Guid Id, Guid FoodItemId, string FoodName, decimal PortionCount, bool IsAlternative);

    private sealed record OgunOzeti(Guid Id, int DayNo, string Name, List<SatirOzeti> Items, Toplamlar Totals);

    private sealed record GunToplami(int DayNo, Toplamlar Totals);

    private sealed record PlanOzeti(Guid Id, Guid ClientId, string Title, string Status, Guid? SourceTemplateId,
        List<OgunOzeti> Meals, List<GunToplami> Days, Toplamlar Totals, string Disclaimer);

    private sealed record SablonOzeti(Guid Id, string Name, List<OgunOzeti> Meals, Toplamlar Totals);
}
