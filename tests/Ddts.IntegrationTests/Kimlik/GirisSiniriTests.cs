using System.Net;
using System.Net.Http.Json;

namespace Ddts.IntegrationTests.Kimlik;

/// <summary>
/// IT-04 (NFR-G2): giriş ucuna IP başına dakikada en fazla 5 deneme; 6. denemede 429.
/// Kendi API örneğini (ApiFactory) kullanır ki diğer testlerin girişleri sayaca karışmasın.
/// </summary>
public sealed class GirisSiniriTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Altinci_giris_denemesi_429_doner()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = factory.CreateClient();
        var durumlar = new List<HttpStatusCode>();

        for (var deneme = 1; deneme <= 6; deneme++)
        {
            var yanit = await client.PostAsJsonAsync(
                "/api/auth/login",
                new { email = "olmayan.kullanici@ornek.test", password = "YanlisParola1" },
                ct);
            durumlar.Add(yanit.StatusCode);
        }

        Assert.All(durumlar.Take(5), durum => Assert.Equal(HttpStatusCode.Unauthorized, durum));
        Assert.Equal(HttpStatusCode.TooManyRequests, durumlar[5]);
    }
}
