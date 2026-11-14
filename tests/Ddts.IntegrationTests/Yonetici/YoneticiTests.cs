using System.Net;
using System.Net.Http.Json;
using Ddts.Application.Subscriptions;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Yonetici;

/// <summary>
/// Yönetici paneli (FR-L1, FR-L5) ve süresi dolan abonelik (FR-L4).
/// KT-09 (paket tanımlama), IT-12 (denetim kayıtları ayrıntısız), IT-02 (rol → 403).
/// Not: giriş ucu dakikada 5 denemeyle sınırlı; bu sınıftaki testler toplam 5 giriş yapar.
/// </summary>
public sealed class YoneticiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Yonetici_paket_tanimlar_duzenler_ve_satistan_kaldirir()
    {
        using var yonetici = await YoneticiGirisiAsync();                                           // giriş 1

        var ilkListe = await yonetici.GetFromJsonAsync<List<Paket>>("/api/admin/subscription-plans", Iptal);
        Assert.Equal(new[] { "Başlangıç", "Standart", "Profesyonel" }, ilkListe!.Select(p => p.Name));

        var ekleme = await yonetici.PostAsJsonAsync(
            "/api/admin/subscription-plans", new { name = "Kurumsal", maxClients = 200, monthlyPrice = 1999m, isActive = true }, Iptal);
        Assert.Equal(HttpStatusCode.Created, ekleme.StatusCode);
        var yeni = (await ekleme.Content.ReadFromJsonAsync<Paket>(Iptal))!;
        Assert.EndsWith($"/api/admin/subscription-plans/{yeni.Id}", ekleme.Headers.Location!.ToString());

        var ayniAd = await yonetici.PostAsJsonAsync(
            "/api/admin/subscription-plans", new { name = "kurumsal", maxClients = 50, monthlyPrice = 10m }, Iptal);
        var sifirLimit = await yonetici.PostAsJsonAsync(
            "/api/admin/subscription-plans", new { name = "Hatalı", maxClients = 0, monthlyPrice = 10m }, Iptal);
        Assert.Equal(HttpStatusCode.Conflict, ayniAd.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, sifirLimit.StatusCode);

        // Düzenleme ve satıştan kaldırma
        var duzenleme = await yonetici.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{yeni.Id}", new { name = "Kurumsal", maxClients = 250, monthlyPrice = 2499m, isActive = false }, Iptal);
        Assert.Equal(HttpStatusCode.OK, duzenleme.StatusCode);
        Assert.Equal(new Paket(yeni.Id, "Kurumsal", 250, 2499m, false), await duzenleme.Content.ReadFromJsonAsync<Paket>(Iptal));

        var bilinmeyen = await yonetici.PutAsJsonAsync(
            $"/api/admin/subscription-plans/{Guid.NewGuid()}", new { name = "Yok", maxClients = 5, monthlyPrice = 1m }, Iptal);
        Assert.Equal(HttpStatusCode.NotFound, bilinmeyen.StatusCode);

        // Satıştan kalkan paket kayıt ekranında görünmez, yönetici listesinde durur
        using var anonim = factory.CreateClient();
        var satistakiler = await anonim.GetFromJsonAsync<List<Paket>>("/api/subscription-plans", Iptal);
        var hepsi = await yonetici.GetFromJsonAsync<List<Paket>>("/api/admin/subscription-plans", Iptal);
        Assert.DoesNotContain(satistakiler!, p => p.Name == "Kurumsal");
        Assert.Contains(hepsi!, p => p.Name == "Kurumsal" && !p.IsActive);

        // Diyetisyen yönetici uçlarını kullanamaz (IT-02)
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var yetkisiz = await diyetisyen.Http.GetAsync("/api/admin/subscription-plans", Iptal);
        Assert.Equal(HttpStatusCode.Forbidden, yetkisiz.StatusCode);
    }

    [Fact]
    public async Task Yonetici_diyetisyen_hesabini_kapatinca_giris_ve_token_gecmez()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        using var yonetici = await YoneticiGirisiAsync();                                           // giriş 2

        var satir = await DiyetisyenSatiriAsync(yonetici, diyetisyen.Eposta);
        Assert.True(satir.IsActive);
        Assert.Equal("Başlangıç", satir.PlanName);
        Assert.Equal("Active", satir.SubscriptionStatus);
        Assert.Equal(new Kullanim(0, 10, false), satir.Usage);

        var kapat = await yonetici.PostAsync($"/api/admin/dietitians/{satir.Id}/deactivate", null, Iptal);
        var tekrarKapat = await yonetici.PostAsync($"/api/admin/dietitians/{satir.Id}/deactivate", null, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, kapat.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, tekrarKapat.StatusCode);

        var eskiToken = await diyetisyen.Http.GetAsync("/api/subscription", Iptal);
        var giris = await GirisAsync(factory, diyetisyen.Eposta);                                  // giriş 3
        Assert.Equal(HttpStatusCode.Unauthorized, eskiToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, giris.StatusCode);

        var ac = await yonetici.PostAsync($"/api/admin/dietitians/{satir.Id}/activate", null, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, ac.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GirisAsync(factory, diyetisyen.Eposta)).StatusCode);   // giriş 4

        var bilinmeyen = await yonetici.PostAsync($"/api/admin/dietitians/{Guid.NewGuid()}/activate", null, Iptal);
        Assert.Equal(HttpStatusCode.NotFound, bilinmeyen.StatusCode);
    }

    [Fact]
    public async Task Yonetici_kullanimi_ve_ayrintisiz_denetim_kayitlarini_gorur()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
        await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
        var guncelleme = await diyetisyen.Http.PutAsJsonAsync(
            $"/api/clients/{danisanId}", new { fullName = "Deneme Danışan", goalType = "Maintain", targetWeightKg = 60 }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, guncelleme.StatusCode);

        using var yonetici = await YoneticiGirisiAsync();                                           // giriş 5
        var satir = await DiyetisyenSatiriAsync(yonetici, diyetisyen.Eposta);

        var kullanim = await yonetici.GetFromJsonAsync<Sayfa<KullanimSatiri>>("/api/admin/usage?pageSize=100", Iptal);
        Assert.Equal(
            new KullanimSatiri(satir.Id, "Dyt. Deneme Örnek", "Başlangıç", new Kullanim(2, 10, false)),
            kullanim!.Items.Single(k => k.DietitianId == satir.Id));

        // Diyetisyenin işlemleri: kayıt, 2 danışan ekleme, 1 güncelleme; en yenisi önce, sayfa başına 2
        var adres = $"/api/admin/audit-logs?userId={satir.UserId}";
        var yanit = await yonetici.GetAsync($"{adres}&pageSize=2", Iptal);
        var ham = await yanit.Content.ReadAsStringAsync(Iptal);
        var sayfa = await yanit.Content.ReadFromJsonAsync<Sayfa<DenetimSatiri>>(Iptal);

        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        Assert.Equal(4, sayfa!.TotalCount);
        Assert.Equal(new[] { "ClientUpdated", "ClientAdded" }, sayfa.Items.Select(i => i.Action));
        Assert.All(sayfa.Items, i => Assert.Equal(satir.UserId, i.UserId));

        // Ayrıntı alanı (eski/yeni değerler) yöneticiye hiç gelmez (belge 2.5, madde 5)
        Assert.DoesNotContain("details", ham, StringComparison.OrdinalIgnoreCase);

        var dun = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var yarin = dun.AddDays(2);
        Assert.Equal(1, (await DenetimSayfasiAsync(yonetici, $"{adres}&action=ClientUpdated")).TotalCount);
        Assert.Equal(4, (await DenetimSayfasiAsync(yonetici, $"{adres}&from={dun:yyyy-MM-dd}&to={yarin:yyyy-MM-dd}")).TotalCount);
        Assert.Equal(0, (await DenetimSayfasiAsync(yonetici, $"{adres}&from={yarin:yyyy-MM-dd}")).TotalCount);
        Assert.Equal(0, (await DenetimSayfasiAsync(yonetici, $"{adres}&to={dun:yyyy-MM-dd}")).TotalCount);

        // Sayfa sınırları ve rol kontrolü
        Assert.Equal(HttpStatusCode.BadRequest, (await yonetici.GetAsync($"{adres}&pageSize=101", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await yonetici.GetAsync($"{adres}&page=0", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await diyetisyen.Http.GetAsync("/api/admin/audit-logs", Iptal)).StatusCode);
    }

    [Fact]
    public async Task Suresi_dolan_abonelikte_danisan_eklenemez_yenileyince_eklenir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var bugun = DateOnly.FromDateTime(DateTime.UtcNow);

        // Aboneliği 10 gün önce bitmiş gibi göster, sonra ReminderWorker'ın yaptığı işi doğrudan çalıştır
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();
            var dietitianId = await DiyetisyenKimligiAsync(db, diyetisyen.Eposta);
            var baslangic = bugun.AddDays(-40);
            var bitis = bugun.AddDays(-10);
            await db.Database.ExecuteSqlAsync(
                $"UPDATE subscriptions SET start_date = {baslangic}, end_date = {bitis} WHERE dietitian_id = {dietitianId}",
                Iptal);

            var biten = await scope.ServiceProvider.GetRequiredService<ISubscriptionService>().ExpireDueAsync(Iptal);
            Assert.True(biten >= 1);
        }

        var abonelik = await diyetisyen.Http.GetFromJsonAsync<Abonelik>("/api/subscription", Iptal);
        Assert.Equal("Expired", abonelik!.Status);

        var ekleme = await DanisanEkleIstegiAsync(diyetisyen.Http, YeniEposta("danisan"));
        Assert.Equal(HttpStatusCode.Conflict, ekleme.StatusCode);
        var problem = await ekleme.Content.ReadFromJsonAsync<ProblemYaniti>(Iptal);
        Assert.StartsWith("Aboneliğin aktif değil.", problem!.Detail);

        // Ödeme simülasyonuyla yenileme: abonelik yeniden aktif, danışan eklenebilir (FR-L4)
        var yenileme = await diyetisyen.Http.PostAsync("/api/subscription/renew", null, Iptal);
        var yenilenen = await yenileme.Content.ReadFromJsonAsync<Abonelik>(Iptal);
        Assert.Equal(HttpStatusCode.OK, yenileme.StatusCode);
        Assert.Equal("Active", yenilenen!.Status);
        Assert.True(yenilenen.EndDate > bugun);

        await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));
    }

    private Task<HttpClient> YoneticiGirisiAsync() =>
        GirisYapmisIstemciAsync(factory, ApiFactory.AdminEmail, ApiFactory.AdminPassword);

    private static async Task<DiyetisyenSatiri> DiyetisyenSatiriAsync(HttpClient yonetici, string eposta)
    {
        var sayfa = await yonetici.GetFromJsonAsync<Sayfa<DiyetisyenSatiri>>("/api/admin/dietitians?pageSize=100", Iptal);
        return sayfa!.Items.Single(d => d.Email == eposta);
    }

    private static async Task<Sayfa<DenetimSatiri>> DenetimSayfasiAsync(HttpClient yonetici, string adres) =>
        (await yonetici.GetFromJsonAsync<Sayfa<DenetimSatiri>>(adres, Iptal))!;

    private static async Task<Guid> DiyetisyenKimligiAsync(DdtsDbContext db, string eposta)
    {
        var kullaniciId = await db.Users.Where(u => u.Email == eposta).Select(u => u.Id).SingleAsync(Iptal);
        return await db.Dietitians.Where(d => d.UserId == kullaniciId).Select(d => d.Id).SingleAsync(Iptal);
    }

    private sealed record Paket(Guid Id, string Name, int MaxClients, decimal MonthlyPrice, bool IsActive);

    private sealed record Sayfa<T>(List<T> Items, int Page, int PageSize, int TotalCount);

    private sealed record DiyetisyenSatiri(
        Guid Id, Guid UserId, string FullName, string Email, bool IsActive, string? PlanName, string? SubscriptionStatus, Kullanim Usage);

    private sealed record KullanimSatiri(Guid DietitianId, string FullName, string? PlanName, Kullanim Usage);

    private sealed record DenetimSatiri(long Id, DateTime At, Guid? UserId, string Action, string EntityType, Guid? EntityId);

    private sealed record Abonelik(string PlanName, string Status, DateOnly EndDate);
}
