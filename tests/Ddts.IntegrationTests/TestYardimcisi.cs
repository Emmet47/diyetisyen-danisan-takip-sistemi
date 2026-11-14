using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Ddts.IntegrationTests;

/// <summary>
/// Entegrasyon testlerinde tekrar eden adımlar: diyetisyen hesabı açma, danışan ekleme, daveti kabul etme.
/// Bütün veriler sahtedir (gerçek kişi ya da sağlık bilgisi yok).
/// </summary>
internal static class TestYardimcisi
{
    public const string Parola = "Demo!2026";

    public static CancellationToken Iptal => TestContext.Current.CancellationToken;

    public static string YeniEposta(string onek) => $"{onek}-{Guid.NewGuid():N}@ornek.test";

    /// <summary>Yeni diyetisyen hesabı açar (varsayılan Başlangıç paketi); dönen istemci o diyetisyenin token'ını taşır.</summary>
    public static async Task<DiyetisyenOturumu> DiyetisyenAcAsync(ApiFactory factory, string paketAdi = "Başlangıç")
    {
        var http = factory.CreateClient();
        var paketler = await http.GetFromJsonAsync<List<PaketBilgisi>>("/api/subscription-plans", Iptal);
        var paket = paketler!.Single(p => p.Name == paketAdi);
        var eposta = YeniEposta("dyt");

        var yanit = await http.PostAsJsonAsync(
            "/api/auth/register",
            new { fullName = "Dyt. Deneme Örnek", email = eposta, password = Parola, subscriptionPlanId = paket.Id },
            Iptal);
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);

        var giris = await yanit.Content.ReadFromJsonAsync<GirisYaniti>(Iptal);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", giris!.AccessToken);
        return new DiyetisyenOturumu(http, eposta);
    }

    /// <summary>docs/api.md 8.3'teki örnek forma benzer sahte danışan.</summary>
    public static object DanisanFormu(string eposta, bool rizaAlindi = true) => new
    {
        fullName = "Deneme Danışan",
        email = eposta,
        phone = "05550000000",
        birthDate = "1996-01-01",
        sex = "Female",
        goalType = "LoseWeight",
        targetWeightKg = 65.0m,
        preferences = "Kahvaltıda tatlı sevmiyor",
        allergyNotes = "Fıstık alerjisi",
        kvkkConsentGiven = rizaAlindi,
    };

    public static Task<HttpResponseMessage> DanisanEkleIstegiAsync(HttpClient diyetisyen, string eposta) =>
        diyetisyen.PostAsJsonAsync("/api/clients", DanisanFormu(eposta), Iptal);

    public static async Task<Guid> DanisanEkleAsync(HttpClient diyetisyen, string eposta)
    {
        var yanit = await DanisanEkleIstegiAsync(diyetisyen, eposta);
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return (await yanit.Content.ReadFromJsonAsync<DanisanEklemeYaniti>(Iptal))!.Id;
    }

    /// <summary>Davet e-postasındaki bağlantıyla danışanın parolasını belirler (FR-A3).</summary>
    public static async Task DavetiKabulEtAsync(ApiFactory factory, string eposta)
    {
        using var http = factory.CreateClient();
        var token = factory.Emails.LastTokenFor(eposta);

        var yanit = await http.PostAsJsonAsync("/api/auth/reset-password", new { token, newPassword = Parola }, Iptal);
        Assert.Equal(HttpStatusCode.NoContent, yanit.StatusCode);
    }

    /// <summary>Giriş denemesi. Giriş ucu dakikada 5 denemeyle sınırlı: her test sınıfı bunu hesaba katar.</summary>
    public static async Task<HttpResponseMessage> GirisAsync(ApiFactory factory, string eposta, string parola = Parola)
    {
        using var http = factory.CreateClient();
        return await http.PostAsJsonAsync("/api/auth/login", new { email = eposta, password = parola }, Iptal);
    }

    /// <summary>Başarılı girişten sonra token'ı taşıyan istemci.</summary>
    public static async Task<HttpClient> GirisYapmisIstemciAsync(ApiFactory factory, string eposta, string parola = Parola)
    {
        var yanit = await GirisAsync(factory, eposta, parola);
        Assert.Equal(HttpStatusCode.OK, yanit.StatusCode);
        var giris = await yanit.Content.ReadFromJsonAsync<GirisYaniti>(Iptal);

        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", giris!.AccessToken);
        return http;
    }
}

internal sealed record DiyetisyenOturumu(HttpClient Http, string Eposta);

internal sealed record PaketBilgisi(Guid Id, string Name, int MaxClients);

internal sealed record GirisYaniti(string AccessToken, DateTime ExpiresAt, string Role);

internal sealed record Kullanim(int ActiveClients, int MaxClients, bool NearLimit);

internal sealed record DanisanEklemeYaniti(Guid Id, Kullanim Usage);

internal sealed record ProblemYaniti(string? Type, string? Title, int Status, string? Detail);
