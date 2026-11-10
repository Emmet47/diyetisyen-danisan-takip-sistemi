using Ddts.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// EF Core veritabanı bağlamı (Şekil 4.10 ve 7.1'deki DdtsDbContext).
/// Tablo eşlemeleri Persistence/Configurations klasöründeki IEntityTypeConfiguration sınıflarından okunur.
/// Tablo ve sütun adları snake_case'e çevrilir (EFCore.NamingConventions, Program.cs'te açılıyor);
/// referans şema: docs/db/schema.sql.
/// </summary>
public sealed class DdtsDbContext(DbContextOptions<DdtsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DdtsDbContext).Assembly);

        // Bekleyen domain event listesi veritabanına yazılmaz.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(AggregateRoot).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(AggregateRoot.DomainEvents));
        }
    }
}
