using System.Net;
using System.Net.Http.Json;
using Ddts.Domain.Foods;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Danisan;

/// <summary>
/// Danışan ekleme, davet, profil, değişiklik geçmişi ve kısıt listesi (B modülü).
/// IT-01 (başka diyetisyenin danışanı → 404), IT-02 (rol → 403), IT-12 (geçmiş), KT-02 (danışan ekle).
/// Not: giriş ucu dakikada 5 denemeyle sınırlı; bu sınıftaki testler toplam 1 giriş yapar.
/// </summary>
public sealed class DanisanTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Danisan_eklenir_davetle_parolasini_belirler_ve_kendi_profilini_gorur()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var eposta = YeniEposta("danisan");

        var yanit = await DanisanEkleIstegiAsync(diyetisyen.Http, eposta);

        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        var eklenen = (await yanit.Content.ReadFromJsonAsync<DanisanEklemeYaniti>(Iptal))!;
        Assert.EndsWith($"/api/clients/{eklenen.Id}", yanit.Headers.Location!.ToString());
        Assert.Equal(new Kullanim(1, 10, false), eklenen.Usage);

        // Davet e-postası gitti; bağlantıyla parola belirlenir, danışan giriş yapar (FR-B1, FR-A3)
        Assert.Contains(factory.Emails.Sent, e => e.To == eposta && e.Kind == "Invitation");
        await DavetiKabulEtAsync(factory, eposta);
        using var danisan = await GirisYapmisIstemciAsync(factory, eposta);

        var profil = await danisan.GetFromJsonAsync<DanisanDetayi>("/api/me/profile", Iptal);
        Assert.Equal(eklenen.Id, profil!.Id);
        Assert.Equal("Deneme Danışan", profil.FullName);
        Assert.Equal(eposta, profil.Email);

        // Danışan diyetisyen uçlarını kullanamaz (IT-02)
        var liste = await danisan.GetAsync("/api/clients", Iptal);
        Assert.Equal(HttpStatusCode.Forbidden, liste.StatusCode);

        // Diyetisyen de danışan uçlarını kullanamaz
        var diyetisyenProfili = await diyetisyen.Http.GetAsync("/api/me/profile", Iptal);
        Assert.Equal(HttpStatusCode.Forbidden, diyetisyenProfili.StatusCode);
    }

    [Fact]
    public async Task Acik_riza_isaretlenmeden_ya_da_eksik_alanla_danisan_eklenemez()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);

        var rizasiz = await diyetisyen.Http.PostAsJsonAsync("/api/clients", DanisanFormu(YeniEposta("danisan"), rizaAlindi: false), Iptal);
        var dogumTarihsiz = await diyetisyen.Http.PostAsJsonAsync(
            "/api/clients",
            new { fullName = "Deneme Danışan", email = YeniEposta("danisan"), sex = "Female", goalType = "Maintain", kvkkConsentGiven = true },
            Iptal);

        Assert.Equal(HttpStatusCode.BadRequest, rizasiz.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, dogumTarihsiz.StatusCode);

        var liste = await diyetisyen.Http.GetFromJsonAsync<List<DanisanSatiri>>("/api/clients", Iptal);
        Assert.Empty(liste!);
    }

    [Fact]
    public async Task Kayitli_e_posta_ile_danisan_eklenemez()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var eposta = YeniEposta("danisan");
        await DanisanEkleAsync(diyetisyen.Http, eposta);

        var ikinci = await DanisanEkleIstegiAsync(diyetisyen.Http, eposta.ToUpperInvariant());
        var diyetisyenEpostasi = await DanisanEkleIstegiAsync(diyetisyen.Http, diyetisyen.Eposta);

        Assert.Equal(HttpStatusCode.Conflict, ikinci.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, diyetisyenEpostasi.StatusCode);
    }

    [Fact]
    public async Task Baska_diyetisyenin_danisanina_giden_her_istek_404()
    {
        var a = await DiyetisyenAcAsync(factory);
        var b = await DiyetisyenAcAsync(factory);
        var danisanA = await DanisanEkleAsync(a.Http, YeniEposta("danisan"));
        var adres = $"/api/clients/{danisanA}";

        var yanitlar = new[]
        {
            await b.Http.GetAsync(adres, Iptal),
            await b.Http.PutAsJsonAsync(adres, new { fullName = "Değiştirildi", goalType = "Maintain" }, Iptal),
            await b.Http.GetAsync($"{adres}/history", Iptal),
            await b.Http.PutAsJsonAsync($"{adres}/restrictions", new { restrictions = Array.Empty<object>() }, Iptal),
            await b.Http.PostAsync($"{adres}/deactivate", null, Iptal),
            await b.Http.PostAsync($"{adres}/activate", null, Iptal),
            await b.Http.DeleteAsync(adres, Iptal),
        };

        Assert.All(yanitlar, y => Assert.Equal(HttpStatusCode.NotFound, y.StatusCode));

        // A'nın danışanı yerinde ve değişmemiş; B'nin listesinde görünmüyor
        var detay = await a.Http.GetFromJsonAsync<DanisanDetayi>(adres, Iptal);
        Assert.Equal("Deneme Danışan", detay!.FullName);
        Assert.True(detay.IsActive);
        Assert.Empty((await b.Http.GetFromJsonAsync<List<DanisanSatiri>>("/api/clients", Iptal))!);
    }

    [Fact]
    public async Task Guncelleme_eski_ve_yeni_degerle_gecmise_yazilir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
        var adres = $"/api/clients/{danisanId}";

        // Telefon, tercih ve alerji notu aynı; ad ve hedef kilo değişiyor (65 → 62)
        var guncelleme = await diyetisyen.Http.PutAsJsonAsync(adres, new
        {
            fullName = "Deneme Danışan Yeni",
            phone = "05550000000",
            goalType = "LoseWeight",
            targetWeightKg = 62,
            preferences = "Kahvaltıda tatlı sevmiyor",
            allergyNotes = "Fıstık alerjisi",
        }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, guncelleme.StatusCode);

        var gecmis = (await diyetisyen.Http.GetFromJsonAsync<List<GecmisSatiri>>($"{adres}/history", Iptal))!;

        Assert.Equal(new[] { "ClientUpdated", "ClientAdded" }, gecmis.Select(g => g.Action));
        Assert.NotNull(gecmis[0].UserId);
        Assert.Equal(
            new[] { new AlanDegisikligi("fullName", "Deneme Danışan", "Deneme Danışan Yeni"), new AlanDegisikligi("targetWeightKg", "65.0", "62.0") },
            gecmis[0].Changes.OrderBy(c => c.Field, StringComparer.Ordinal));
        Assert.Empty(gecmis[1].Changes);

        var detay = await diyetisyen.Http.GetFromJsonAsync<DanisanDetayi>(adres, Iptal);
        Assert.Equal("Deneme Danışan Yeni", detay!.FullName);
    }

    [Fact]
    public async Task Kisit_listesi_kaydedilir_degisir_ve_gecmise_yazilir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var baskaDiyetisyen = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
        var adres = $"/api/clients/{danisanId}";

        var (yerFistigi, sut, baskasininBesini) = await BesinleriEkleAsync(baskaDiyetisyen.Eposta);

        var ilk = await diyetisyen.Http.PutAsJsonAsync($"{adres}/restrictions", new
        {
            restrictions = new[]
            {
                new { foodItemId = yerFistigi, type = "Allergy" },
                new { foodItemId = sut, type = "Dislike" },
            },
        }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, ilk.StatusCode);

        var detay = await diyetisyen.Http.GetFromJsonAsync<DanisanDetayi>(adres, Iptal);
        Assert.Equal(
            new[] { new KisitSatiri(sut, "Süt", "Dislike"), new KisitSatiri(yerFistigi, "Yer fıstığı", "Allergy") },
            detay!.Restrictions);

        // Süt listeden çıkar, yer fıstığının türü değişir
        var ikinci = await diyetisyen.Http.PutAsJsonAsync($"{adres}/restrictions", new
        {
            restrictions = new[] { new { foodItemId = yerFistigi, type = "Dislike" } },
        }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, ikinci.StatusCode);

        detay = await diyetisyen.Http.GetFromJsonAsync<DanisanDetayi>(adres, Iptal);
        Assert.Equal(new[] { new KisitSatiri(yerFistigi, "Yer fıstığı", "Dislike") }, detay!.Restrictions);

        // Başka diyetisyenin kendi besini ve aynı besinin iki kez yazılması reddedilir
        var baskasinin = await diyetisyen.Http.PutAsJsonAsync($"{adres}/restrictions", new
        {
            restrictions = new[] { new { foodItemId = baskasininBesini, type = "Allergy" } },
        }, Iptal);
        var tekrarli = await diyetisyen.Http.PutAsJsonAsync($"{adres}/restrictions", new
        {
            restrictions = new[] { new { foodItemId = sut, type = "Allergy" }, new { foodItemId = sut, type = "Dislike" } },
        }, Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, baskasinin.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tekrarli.StatusCode);

        var gecmis = (await diyetisyen.Http.GetFromJsonAsync<List<GecmisSatiri>>($"{adres}/history", Iptal))!;
        var sonDegisiklik = gecmis.First(g => g.Action == "ClientRestrictionsUpdated").Changes.Single();
        Assert.Equal(new AlanDegisikligi("restrictions", "Süt (Dislike), Yer fıstığı (Allergy)", "Yer fıstığı (Dislike)"), sonDegisiklik);
        Assert.Equal(2, gecmis.Count(g => g.Action == "ClientRestrictionsUpdated"));
    }

    /// <summary>İki ortak besin ve başka diyetisyenin kendi eklediği bir besin (sahte değerler).</summary>
    private async Task<(Guid YerFistigi, Guid Sut, Guid BaskasininBesini)> BesinleriEkleAsync(string baskaDiyetisyenEpostasi)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();

        var kullaniciId = await db.Users.Where(u => u.Email == baskaDiyetisyenEpostasi).Select(u => u.Id).SingleAsync(Iptal);
        var baskaDiyetisyenId = await db.Dietitians.Where(d => d.UserId == kullaniciId).Select(d => d.Id).SingleAsync(Iptal);

        var yerFistigi = new FoodItem(null, "Yer fıstığı", "1 avuç", 30m, 567m, 25.8m, 16.1m, 49.2m, "Yerel", null, null);
        var sut = new FoodItem(null, "Süt", "1 su bardağı", 200m, 61m, 3.2m, 4.8m, 3.3m, "Yerel", null, null);
        var ozelBesin = new FoodItem(baskaDiyetisyenId, "Özel tarif", "1 porsiyon", 100m, 150m, 5m, 20m, 5m, "Yerel", null, null);

        db.FoodItems.AddRange(yerFistigi, sut, ozelBesin);
        await db.SaveChangesAsync(Iptal);

        return (yerFistigi.Id, sut.Id, ozelBesin.Id);
    }

    private sealed record DanisanSatiri(Guid Id, string FullName, bool IsActive);

    private sealed record DanisanDetayi(Guid Id, string FullName, string Email, bool IsActive, List<KisitSatiri> Restrictions);

    private sealed record KisitSatiri(Guid FoodItemId, string FoodName, string Type);

    private sealed record GecmisSatiri(DateTime At, string Action, Guid? UserId, List<AlanDegisikligi> Changes);

    private sealed record AlanDegisikligi(string Field, string? OldValue, string? NewValue);
}
