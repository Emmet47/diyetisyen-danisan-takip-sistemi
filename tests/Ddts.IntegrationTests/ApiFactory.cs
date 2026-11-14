using Ddts.Application.Abstractions.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace Ddts.IntegrationTests;

/// <summary>
/// Testler için API'yi bellekte başlatır. Her test sınıfı için geçici bir PostgreSQL 18
/// konteyneri açılır ve testler bitince silinir; gerçek geliştirme veritabanına dokunulmaz.
/// E-postalar gönderilmez, <see cref="Emails"/> listesinde toplanır.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@ornek.test";
    public const string AdminPassword = "Yonetici!2026";

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine").Build();

    /// <summary>API'nin "gönderdiği" e-postalar (ör. parola sıfırlama bağlantısı).</summary>
    public TestEmailSender Emails { get; } = new();

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        // Test veritabanı boş açılır; migration'lar uygulansın ki tablolar oluşsun.
        builder.UseSetting("Database:MigrateOnStartup", "true");
        // Yalnızca testler için imza anahtarı (gerçek anahtar repoda tutulmaz)
        builder.UseSetting("Jwt:SigningKey", "yalnizca-test-icin-imza-anahtari-en-az-32-karakter");
        builder.UseSetting("Seed:AdminEmail", AdminEmail);
        builder.UseSetting("Seed:AdminPassword", AdminPassword);
        builder.UseSetting("Web:BaseUrl", "http://localhost:5173");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
