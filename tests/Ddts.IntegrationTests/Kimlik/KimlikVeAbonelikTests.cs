using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ddts.IntegrationTests.Kimlik;

/// <summary>
/// Kayıt, giriş, rol kontrolü, parola sıfırlama ve abonelik uçları (A ve L modülleri).
/// IT-02 (rol → 403), IT-04 (giriş, JWT), IT-14 (parola sıfırlama), KT-01 (hesap aç ve paket seç).
/// Not: giriş ucu dakikada 5 denemeyle sınırlı; bu sınıftaki testler toplam 4 giriş yapar.
/// </summary>
public sealed class KimlikVeAbonelikTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Parola = "Demo!2026";

    [Fact]
    public async Task Paketler_herkese_acik_ve_limite_gore_sirali()
    {
        using var client = factory.CreateClient();

        var paketler = await PaketlerAsync(client);

        Assert.Equal(new[] { "Başlangıç", "Standart", "Profesyonel" }, paketler.Select(p => p.Name));
        Assert.Equal(new[] { 10, 30, 100 }, paketler.Select(p => p.MaxClients));
    }

    [Fact]
    public async Task Kayit_olan_diyetisyen_hemen_aboneligini_gorur()
    {
        using var client = factory.CreateClient();
        var eposta = YeniEposta();

        var kayit = await KayitAsync(client, eposta, "Başlangıç");
        Assert.Equal("Dietitian", kayit.Role);
        Assert.True(kayit.ExpiresAt > DateTime.UtcNow.AddMinutes(55));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kayit.AccessToken);
        var abonelik = await client.GetFromJsonAsync<AbonelikYaniti>("/api/subscription", TestContext.Current.CancellationToken);

        Assert.NotNull(abonelik);
        Assert.Equal("Başlangıç", abonelik.PlanName);
        Assert.Equal("Active", abonelik.Status);
        Assert.Equal(new KullanimYaniti(0, 10, false), abonelik.Usage);
    }

    [Fact]
    public async Task Ayni_e_posta_ile_ikinci_kayit_409()
    {
        using var client = factory.CreateClient();
        var eposta = YeniEposta();
        await KayitAsync(client, eposta, "Başlangıç");

        var yanit = await KayitIstegiAsync(client, eposta.ToUpperInvariant(), "Standart");

        Assert.Equal(HttpStatusCode.Conflict, yanit.StatusCode);
    }

    [Fact]
    public async Task Dogru_parola_ile_giris_200_yanlis_parola_ile_401()
    {
        using var client = factory.CreateClient();
        var eposta = YeniEposta();
        await KayitAsync(client, eposta, "Başlangıç");

        var dogru = await GirisIstegiAsync(client, eposta, Parola);
        var yanlis = await GirisIstegiAsync(client, eposta, "YanlisParola1");

        Assert.Equal(HttpStatusCode.OK, dogru.StatusCode);
        var token = await dogru.Content.ReadFromJsonAsync<TokenYaniti>(TestContext.Current.CancellationToken);
        Assert.False(string.IsNullOrEmpty(token?.AccessToken));
        Assert.Equal(HttpStatusCode.Unauthorized, yanlis.StatusCode);
    }

    [Fact]
    public async Task Token_olmadan_korunan_uca_401()
    {
        using var client = factory.CreateClient();

        var yanit = await client.GetAsync("/api/subscription", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, yanit.StatusCode);
    }

    [Fact]
    public async Task Yonetici_diyetisyen_ucunu_kullanamaz_403()
    {
        using var client = factory.CreateClient();
        var giris = await GirisIstegiAsync(client, ApiFactory.AdminEmail, ApiFactory.AdminPassword);
        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);
        var token = await giris.Content.ReadFromJsonAsync<TokenYaniti>(TestContext.Current.CancellationToken);
        Assert.Equal("Admin", token!.Role);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var yanit = await client.GetAsync("/api/subscription", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, yanit.StatusCode);
    }

    [Fact]
    public async Task Parola_sifirlama_baglantisi_calisir_ve_tek_kullanimliktir()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var eposta = YeniEposta();
        await KayitAsync(client, eposta, "Başlangıç");

        // Kayıtlı olmayan e-postada da 202: hangi e-postanın kayıtlı olduğu öğrenilemez
        var bilinmeyen = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = YeniEposta() }, ct);
        var kayitli = await client.PostAsJsonAsync("/api/auth/forgot-password", new { email = eposta }, ct);
        Assert.Equal(HttpStatusCode.Accepted, bilinmeyen.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, kayitli.StatusCode);

        var token = factory.Emails.LastTokenFor(eposta);
        var sifirla = await client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "YeniParola!2026" }, ct);
        var ikinciKez = await client.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = "BaskaParola!2026" }, ct);
        var giris = await GirisIstegiAsync(client, eposta, "YeniParola!2026");

        Assert.Equal(HttpStatusCode.NoContent, sifirla.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, ikinciKez.StatusCode);
        Assert.Equal(HttpStatusCode.OK, giris.StatusCode);
    }

    [Fact]
    public async Task Paket_yukseltilince_yeni_paket_aktif_olur()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var kayit = await KayitAsync(client, YeniEposta(), "Başlangıç");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", kayit.AccessToken);
        var standart = (await PaketlerAsync(client)).Single(p => p.Name == "Standart");

        var degistir = await client.PostAsJsonAsync("/api/subscription/change", new { subscriptionPlanId = standart.Id }, ct);
        var ayniPaket = await client.PostAsJsonAsync("/api/subscription/change", new { subscriptionPlanId = standart.Id }, ct);

        Assert.Equal(HttpStatusCode.OK, degistir.StatusCode);
        var abonelik = await degistir.Content.ReadFromJsonAsync<AbonelikYaniti>(ct);
        Assert.Equal("Standart", abonelik!.PlanName);
        Assert.Equal(new KullanimYaniti(0, 30, false), abonelik.Usage);
        Assert.Equal(HttpStatusCode.BadRequest, ayniPaket.StatusCode);
    }

    // ---- yardımcılar ----

    private static string YeniEposta() => $"dyt-{Guid.NewGuid():N}@ornek.test";

    private static async Task<List<PaketYaniti>> PaketlerAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<List<PaketYaniti>>("/api/subscription-plans", TestContext.Current.CancellationToken))!;

    private static async Task<HttpResponseMessage> KayitIstegiAsync(HttpClient client, string eposta, string paketAdi)
    {
        var paket = (await PaketlerAsync(client)).Single(p => p.Name == paketAdi);
        return await client.PostAsJsonAsync(
            "/api/auth/register",
            new { fullName = "Dyt. Deneme Örnek", email = eposta, password = Parola, subscriptionPlanId = paket.Id },
            TestContext.Current.CancellationToken);
    }

    private static async Task<TokenYaniti> KayitAsync(HttpClient client, string eposta, string paketAdi)
    {
        var yanit = await KayitIstegiAsync(client, eposta, paketAdi);
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return (await yanit.Content.ReadFromJsonAsync<TokenYaniti>(TestContext.Current.CancellationToken))!;
    }

    private static Task<HttpResponseMessage> GirisIstegiAsync(HttpClient client, string eposta, string parola) =>
        client.PostAsJsonAsync("/api/auth/login", new { email = eposta, password = parola }, TestContext.Current.CancellationToken);

    private sealed record TokenYaniti(string AccessToken, DateTime ExpiresAt, string Role);

    private sealed record PaketYaniti(Guid Id, string Name, int MaxClients, decimal MonthlyPrice, bool IsActive);

    private sealed record AbonelikYaniti(Guid Id, Guid PlanId, string PlanName, string Status, DateOnly EndDate, KullanimYaniti Usage);

    private sealed record KullanimYaniti(int ActiveClients, int MaxClients, bool NearLimit);
}
