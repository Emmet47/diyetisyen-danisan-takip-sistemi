using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ddts.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    /// <summary>
    /// Bekleyen EF Core migration'larını uygular. Docker ile demo açılırken tabloların
    /// kendiliğinden oluşması için Program.cs'ten çağrılır (Database:MigrateOnStartup=true).
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Henüz hiç migration yazılmadıysa yapacak bir şey yok.
        if (db.Database.GetMigrations().Any())
        {
            await db.Database.MigrateAsync(cancellationToken);
        }
    }
}
