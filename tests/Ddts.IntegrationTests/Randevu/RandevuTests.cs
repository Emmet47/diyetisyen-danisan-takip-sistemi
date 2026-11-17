using System.Net;
using System.Net.Http.Json;
using Ddts.Application.Notifications;
using Ddts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Ddts.IntegrationTests.ModulYardimcisi;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests.Randevu;

/// <summary>
/// Randevu ve bildirimler (I, J modülleri, Şekil 5.7): boş slotlar (FR-I1), talep ve onay (FR-I2), aynı slota
/// eşzamanlı iki talep (IT-05, FR-I4), erteleme/iptal (BR-09), izinli bildirim (FR-J2), 24 saat önce hatırlatma
/// tek sefer (IT-11, BR-12). Zamanlar UTC; slot "yarın 10:00 TSİ" = yarın 07:00 UTC. Bu sınıf toplam 2 giriş yapar.
/// </summary>
public sealed class RandevuTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static DateOnly Yarin => Bugun().AddDays(1);

    /// <summary>Yarın, Türkiye saatiyle verilen saat (UTC'ye çevrilmiş).</summary>
    private static DateTime YarinSaat(int saat, int dakika = 0) =>
        DateTime.SpecifyKind(Yarin.ToDateTime(new TimeOnly(saat, dakika)).AddHours(-3), DateTimeKind.Utc);

    [Fact]
    public async Task Talep_onay_erteleme_iptal_ve_bildirimler_calisir_ayni_slota_iki_talepten_biri_kaydedilir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        await CalismaSaatleriniGirAsync(diyetisyen.Http);
        var ayse = await DanisanHesabiAcAsync(factory, diyetisyen.Http);
        var zeynep = await DanisanHesabiAcAsync(factory, diyetisyen.Http);

        // Boş slotlar: 09:00–17:00 → 16 slot, ilki 09:00 TSİ (06:00 UTC)
        var slotlar = (await ayse.Http.GetFromJsonAsync<List<Slot>>($"/api/me/available-slots?date={Yarin:yyyy-MM-dd}", Iptal))!;
        Assert.Equal(16, slotlar.Count);
        Assert.Equal(YarinSaat(9), slotlar[0].StartsAt);

        // Slot başında olmayan saat 400
        var kayik = await ayse.Http.PostAsJsonAsync("/api/me/appointments", new { startsAt = YarinSaat(10, 15) }, Iptal);
        Assert.Equal(HttpStatusCode.BadRequest, kayik.StatusCode);

        // IT-05: aynı slota aynı anda iki talep → biri 201, diğeri 409 "Bu saat az önce doldu"
        var istekler = await Task.WhenAll(
            ayse.Http.PostAsJsonAsync("/api/me/appointments", new { startsAt = YarinSaat(10) }, Iptal),
            zeynep.Http.PostAsJsonAsync("/api/me/appointments", new { startsAt = YarinSaat(10) }, Iptal));
        Assert.Single(istekler, i => i.StatusCode == HttpStatusCode.Created);
        var reddedilen = Assert.Single(istekler, i => i.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal("Bu saat az önce doldu", (await reddedilen.Content.ReadFromJsonAsync<ProblemYaniti>(Iptal))!.Title);

        var kazanan = istekler[0].StatusCode == HttpStatusCode.Created ? ayse : zeynep;
        var randevu = (await istekler.Single(i => i.StatusCode == HttpStatusCode.Created).Content.ReadFromJsonAsync<RandevuBilgisi>(Iptal))!;
        Assert.Equal("Requested", randevu.Status);
        Assert.Equal(YarinSaat(10).AddMinutes(30), randevu.EndsAt);

        // Dolu slot artık listede yok; diyetisyene talep bildirimi geldi
        var kalan = (await ayse.Http.GetFromJsonAsync<List<Slot>>($"/api/me/available-slots?date={Yarin:yyyy-MM-dd}", Iptal))!;
        Assert.DoesNotContain(kalan, s => s.StartsAt == YarinSaat(10));
        Assert.Contains((await diyetisyen.Http.GetFromJsonAsync<BildirimSayfasi>("/api/notifications", Iptal))!.Items,
            b => b.Type == "AppointmentRequested");

        // Onay → danışana bildirim (FR-J1)
        var onay = await diyetisyen.Http.PostAsync($"/api/appointments/{randevu.Id}/confirm", null, Iptal);
        Assert.Equal(HttpStatusCode.OK, onay.StatusCode);
        Assert.Equal("Confirmed", (await onay.Content.ReadFromJsonAsync<RandevuBilgisi>(Iptal))!.Status);
        var kazananBildirimleri = (await kazanan.Http.GetFromJsonAsync<BildirimSayfasi>("/api/notifications", Iptal))!;
        var onayBildirimi = Assert.Single(kazananBildirimleri.Items, b => b.Type == "AppointmentConfirmed");
        Assert.Equal("Randevun onaylandı", onayBildirimi.Title);
        Assert.Equal(HttpStatusCode.NoContent, (await kazanan.Http.PostAsync($"/api/notifications/{onayBildirimi.Id}/read", null, Iptal)).StatusCode);

        // Danışan erteler → yeniden Talep Edildi (BR-09); başka danışan bu randevuya dokunamaz (404)
        var digeri = kazanan == ayse ? zeynep : ayse;
        Assert.Equal(HttpStatusCode.NotFound,
            (await digeri.Http.PostAsync($"/api/me/appointments/{randevu.Id}/cancel", null, Iptal)).StatusCode);

        var erteleme = await kazanan.Http.PostAsJsonAsync($"/api/me/appointments/{randevu.Id}/reschedule", new { startsAt = YarinSaat(11) }, Iptal);
        Assert.Equal(HttpStatusCode.OK, erteleme.StatusCode);
        Assert.Equal("Requested", (await erteleme.Content.ReadFromJsonAsync<RandevuBilgisi>(Iptal))!.Status);

        // Danışan randevu bildirimlerini kapatırsa iptal bildirimi gelmez (izinli bildirim, FR-J2)
        var izin = await kazanan.Http.PutAsJsonAsync("/api/me/notification-settings",
            new { appointments = false, mealReminders = true, measurements = true, plans = true }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, izin.StatusCode);

        var iptal = await diyetisyen.Http.PostAsync($"/api/appointments/{randevu.Id}/cancel", null, Iptal);
        Assert.Equal(HttpStatusCode.OK, iptal.StatusCode);
        Assert.Equal("Cancelled", (await iptal.Content.ReadFromJsonAsync<RandevuBilgisi>(Iptal))!.Status);
        Assert.DoesNotContain((await kazanan.Http.GetFromJsonAsync<BildirimSayfasi>("/api/notifications", Iptal))!.Items,
            b => b.Type == "AppointmentCancelled");

        // İptal edilmiş randevu tekrar iptal edilemez (400); başka diyetisyen onaylayamaz (404)
        Assert.Equal(HttpStatusCode.BadRequest, (await diyetisyen.Http.PostAsync($"/api/appointments/{randevu.Id}/cancel", null, Iptal)).StatusCode);
        var baskasi = await DiyetisyenAcAsync(factory);
        Assert.Equal(HttpStatusCode.NotFound, (await baskasi.Http.PostAsync($"/api/appointments/{randevu.Id}/confirm", null, Iptal)).StatusCode);

        // Danışan diyetisyen uçlarını kullanamaz (403)
        Assert.Equal(HttpStatusCode.Forbidden, (await ayse.Http.GetAsync("/api/appointments", Iptal)).StatusCode);
    }

    [Fact]
    public async Task Randevudan_24_saat_once_hatirlatma_bir_kez_uretilir()
    {
        var diyetisyen = await DiyetisyenAcAsync(factory);
        await CalismaSaatleriniGirAsync(diyetisyen.Http);
        var danisanId = await DanisanEkleAsync(diyetisyen.Http, YeniEposta("danisan"));

        // Diyetisyen doğrudan onaylı randevu oluşturur (FR-I2)
        var olusan = await diyetisyen.Http.PostAsJsonAsync("/api/appointments", new { clientId = danisanId, startsAt = YarinSaat(14) }, Iptal);
        Assert.Equal(HttpStatusCode.Created, olusan.StatusCode);
        var randevu = (await olusan.Content.ReadFromJsonAsync<RandevuBilgisi>(Iptal))!;
        Assert.Equal("Confirmed", randevu.Status);

        var takvim = (await diyetisyen.Http.GetFromJsonAsync<List<RandevuBilgisi>>("/api/appointments", Iptal))!;
        Assert.Equal("Deneme Danışan", Assert.Single(takvim, r => r.Id == randevu.Id).ClientName);

        // Aynı slota ikinci randevu 409
        var cakisan = await diyetisyen.Http.PostAsJsonAsync("/api/appointments", new { clientId = danisanId, startsAt = YarinSaat(14) }, Iptal);
        Assert.Equal(HttpStatusCode.Conflict, cakisan.StatusCode);

        // ReminderWorker iki kez çalışsa da (randevudan 2 saat önce) hatırlatma bir kez üretilir (IT-11)
        var simdi = randevu.StartsAt.AddHours(-2);
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IReminderService>().SendDueRemindersAsync(simdi, Iptal);
        }

        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IReminderService>().SendDueRemindersAsync(simdi.AddMinutes(10), Iptal);
        }

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();
            var hatirlatmalar = await db.Notifications
                .Where(n => n.DedupKey == $"appt:{randevu.Id}:24h")
                .ToListAsync(Iptal);

            var hatirlatma = Assert.Single(hatirlatmalar);
            Assert.Equal("Randevu hatırlatması", hatirlatma.Title);
            Assert.DoesNotContain("kg", hatirlatma.Body);   // metinde sağlık verisi yok
        }
    }

    /// <summary>Haftanın her günü 09:00–17:00 (Türkiye saati).</summary>
    private static async Task CalismaSaatleriniGirAsync(HttpClient diyetisyen)
    {
        var saatler = Enumerable.Range(1, 7).Select(gun => new { weekday = gun, startTime = "09:00:00", endTime = "17:00:00" });
        var yanit = await diyetisyen.PutAsJsonAsync("/api/working-hours", new { hours = saatler }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);

        var kayitli = (await diyetisyen.GetFromJsonAsync<List<CalismaSaati>>("/api/working-hours", Iptal))!;
        Assert.Equal(7, kayitli.Count);
    }

    private sealed record Slot(DateTime StartsAt, DateTime EndsAt);

    private sealed record RandevuBilgisi(Guid Id, Guid ClientId, string? ClientName, DateTime StartsAt, DateTime EndsAt, string Status);

    private sealed record CalismaSaati(int Weekday, TimeOnly StartTime, TimeOnly EndTime);
}
