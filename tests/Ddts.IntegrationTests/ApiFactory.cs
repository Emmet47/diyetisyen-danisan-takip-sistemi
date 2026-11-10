using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Ddts.IntegrationTests;

/// <summary>
/// Testler için API'yi bellekte başlatır. Her test sınıfı için geçici bir PostgreSQL 18
/// konteyneri açılır ve testler bitince silinir; gerçek geliştirme veritabanına dokunulmaz.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async ValueTask InitializeAsync() => await _database.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", _database.GetConnectionString());
        // Test veritabanı boş açılır; migration'lar uygulansın ki tablolar oluşsun.
        builder.UseSetting("Database:MigrateOnStartup", "true");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();
    }
}
