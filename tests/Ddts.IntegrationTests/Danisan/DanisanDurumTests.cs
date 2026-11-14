using System.Net;
using System.Net.Http.Json;
using Ddts.Domain.Measurements;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Danisan;

/// <summary>
/// Paket limiti, pasife alma / aktifleştirme ve kalıcı silme (FR-B4, FR-L2).
/// IT-03 (limit dolu → 409), IT-13 (kalıcı silme ve KVKK temizliği).
/// Not: giriş ucu dakikada 5 denemeyle sınırlı; bu sınıftaki testler toplam 4 giriş yapar.
/// </summary>
public sealed class DanisanDurumTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Paket_limiti_dolunca_danisan_eklenemez_pasife_alinca_yer_acilir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);   // Başlangıç: en fazla 10 aktif danışan
        var danisanlar = new List<Guid>();

        for (var i = 1; i <= 10; i++)
        {
            var yanit = await DanisanEkleIstegiAsync(diyetisyen.Http, YeniEposta("danisan"));
            Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

            var eklenen = (await yanit.Content.ReadFromJsonAsync<DanisanEklemeYaniti>(Iptal))!;
            danisanlar.Add(eklenen.Id);

            // %80'e (8/10) gelince sarı uyarı (FR-L2)
            Assert.Equal(new Kullanim(i, 10, i >= 8), eklenen.Usage);
        }

        var onBirinci = await DanisanEkleIstegiAsync(diyetisyen.Http, YeniEposta("danisan"));

        Assert.Equal(HttpStatusCode.Conflict, onBirinci.StatusCode);
        var problem = await onBirinci.Content.ReadFromJsonAsync<ProblemYaniti>(Iptal);
        Assert.Equal("https://ddts.example/errors/subscription-limit", problem!.Type);
        Assert.Equal("Paket limiti dolu", problem.Title);
        Assert.StartsWith("Başlangıç paketinde en fazla 10 aktif danışan olabilir.", problem.Detail);

        // Pasif danışan paket sayımına girmez: yer açılır
        var pasif = await diyetisyen.Http.PostAsync($"/api/clients/{danisanlar[0]}/deactivate", null, Iptal);
        var tekrarPasif = await diyetisyen.Http.PostAsync($"/api/clients/{danisanlar[0]}/deactivate", null, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, pasif.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, tekrarPasif.StatusCode);

        await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));

        // Limit yine dolu: pasif danışan aktifleştirilemez (FR-B4)
        var aktif = await diyetisyen.Http.PostAsync($"/api/clients/{danisanlar[0]}/activate", null, Iptal);
        Assert.Equal(HttpStatusCode.Conflict, aktif.StatusCode);

        var hepsi = await diyetisyen.Http.GetFromJsonAsync<List<DanisanSatiri>>("/api/clients", Iptal);
        var aktifler = await diyetisyen.Http.GetFromJsonAsync<List<DanisanSatiri>>("/api/clients?active=true", Iptal);
        Assert.Equal(11, hepsi!.Count);
        Assert.Equal(10, aktifler!.Count);
        Assert.DoesNotContain(aktifler, d => d.Id == danisanlar[0]);
    }

    [Fact]
    public async Task Pasif_danisan_giris_yapamaz_aktiflesince_yapabilir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var eposta = YeniEposta("danisan");
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, eposta);
        await DavetiKabulEtAsync(factory, eposta);
        using var danisan = await GirisYapmisIstemciAsync(factory, eposta);                       // giriş 1

        var pasif = await diyetisyen.Http.PostAsync($"/api/clients/{danisanId}/deactivate", null, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, pasif.StatusCode);

        // Elindeki token da artık geçmez (her istekte hesabın aktif olduğu kontrol edilir)
        var eskiToken = await danisan.GetAsync("/api/me/profile", Iptal);
        var yeniGiris = await GirisAsync(factory, eposta);                                         // giriş 2
        Assert.Equal(HttpStatusCode.Unauthorized, eskiToken.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, yeniGiris.StatusCode);

        var aktif = await diyetisyen.Http.PostAsync($"/api/clients/{danisanId}/activate", null, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, aktif.StatusCode);

        var tekrarGiris = await GirisAsync(factory, eposta);                                       // giriş 3
        Assert.Equal(HttpStatusCode.OK, tekrarGiris.StatusCode);

        var gecmis = await diyetisyen.Http.GetFromJsonAsync<List<GecmisSatiri>>($"/api/clients/{danisanId}/history", Iptal);
        Assert.Equal(new[] { "ClientActivated", "ClientDeactivated", "ClientAdded" }, gecmis!.Select(g => g.Action));
    }

    [Fact]
    public async Task Kalici_silinen_danisanin_kayitlari_gider_denetim_kaydinda_yalnizca_islem_kalir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        var eposta = YeniEposta("danisan");
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, eposta);
        await DavetiKabulEtAsync(factory, eposta);
        var adres = $"/api/clients/{danisanId}";

        // Geçmişe değer ayrıntısı düşsün diye hedef kilo değişir; bir de ölçüm eklenir (sahte değerler)
        var guncelleme = await diyetisyen.Http.PutAsJsonAsync(adres, new { fullName = "Deneme Danışan", goalType = "LoseWeight", targetWeightKg = 60 }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, guncelleme.StatusCode);
        await OlcumEkleAsync(danisanId);

        var silme = await diyetisyen.Http.DeleteAsync(adres, Iptal);

        Assert.Equal(HttpStatusCode.NoContent, silme.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diyetisyen.Http.GetAsync(adres, Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await diyetisyen.Http.GetAsync($"{adres}/history", Iptal)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GirisAsync(factory, eposta)).StatusCode);   // giriş 4

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();

            Assert.False(await db.Users.AnyAsync(u => u.Email == eposta, Iptal));
            Assert.False(await db.Clients.AnyAsync(c => c.Id == danisanId, Iptal));
            Assert.False(await db.Measurements.AnyAsync(m => m.ClientId == danisanId, Iptal));

            // KVKK: "kim, ne zaman, hangi işlem" kalır; eski/yeni değerler silinir (belge 2.5)
            var kayitlar = await db.AuditLogs.Where(a => a.EntityType == "Client" && a.EntityId == danisanId).ToListAsync(Iptal);
            Assert.Equal(
                new[] { "ClientAdded", "ClientDeleted", "ClientUpdated" },
                kayitlar.Select(k => k.Action).Order(StringComparer.Ordinal));
            Assert.All(kayitlar, k => Assert.Null(k.Details));
        }

        // E-posta artık boşta: aynı kişi yeniden eklenebilir
        await DanisanEkleAsync(diyetisyen.Http, eposta);
    }

    private async Task OlcumEkleAsync(Guid danisanId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();

        db.Measurements.Add(new Measurement(danisanId, new DateOnly(2026, 11, 1), 70.4m, 168m, 82m, null, null, null));
        await db.SaveChangesAsync(Iptal);
    }

    private sealed record DanisanSatiri(Guid Id, string FullName, bool IsActive);

    private sealed record GecmisSatiri(DateTime At, string Action, Guid? UserId);
}
