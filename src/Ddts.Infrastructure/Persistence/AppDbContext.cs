using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// EF Core veritabanı bağlamı. Tablo eşlemeleri Persistence/Configurations klasöründeki
/// IEntityTypeConfiguration sınıflarından otomatik okunur.
/// Tablo ve sütun adları snake_case'e çevrilir (EFCore.NamingConventions, Program.cs'te açılıyor).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
