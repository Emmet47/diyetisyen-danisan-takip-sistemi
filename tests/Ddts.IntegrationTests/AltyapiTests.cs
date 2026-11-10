using System.Net;
using Ddts.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Ddts.IntegrationTests;

/// <summary>
/// Örnek entegrasyon testleri: API ayağa kalkıyor mu, veritabanına bağlanabiliyor mu?
/// Gerçek senaryolar (yetkisiz erişim 404, çift randevu 409 ...) bu klasöre eklenecek.
/// </summary>
public sealed class AltyapiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_endpoint_200_doner()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Veritabanina_baglanilabiliyor()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var baglandi = await db.Database.CanConnectAsync(TestContext.Current.CancellationToken);

        Assert.True(baglandi);
    }
}
