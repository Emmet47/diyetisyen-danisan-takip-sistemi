using Ddts.Domain.Appointments;
using Ddts.Domain.Auditing;
using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Foods;
using Ddts.Domain.MealLogs;
using Ddts.Domain.Measurements;
using Ddts.Domain.Notifications;
using Ddts.Domain.Plans;
using Ddts.Domain.Subscriptions;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// EF Core veritabanı bağlamı (Şekil 4.10 ve 7.1'deki DdtsDbContext).
/// Tablo eşlemeleri Persistence/Configurations klasöründeki IEntityTypeConfiguration sınıflarından okunur.
/// Tablo ve sütun adları snake_case'e çevrilir (EFCore.NamingConventions, Program.cs'te açılıyor);
/// referans şema: docs/db/schema.sql. Migration'ın bu şemayla aynı olduğunu
/// tests/Ddts.IntegrationTests/Veritabani/SemaUyumuTests denetler.
/// </summary>
public sealed class DdtsDbContext(DbContextOptions<DdtsDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Dietitian> Dietitians => Set<Dietitian>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<FoodItem> FoodItems => Set<FoodItem>();

    public DbSet<Measurement> Measurements => Set<Measurement>();

    public DbSet<PlanTemplate> PlanTemplates => Set<PlanTemplate>();

    public DbSet<NutritionPlan> NutritionPlans => Set<NutritionPlan>();

    public DbSet<MealLog> MealLogs => Set<MealLog>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // EF Core normalde her yabancı anahtar sütununa otomatik indeks ekler. İndeksleri schema.sql'deki
        // gibi elle tanımlıyoruz (Configurations), böylece migration şemayla birebir aynı kalıyor.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Randevulardaki EXCLUDE kısıtında uuid için "=" karşılaştırması bu eklentiyi ister.
        modelBuilder.HasPostgresExtension("btree_gist");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DdtsDbContext).Assembly);

        // Bekleyen domain event listesi veritabanına yazılmaz.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(t => typeof(AggregateRoot).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(AggregateRoot.DomainEvents));
        }
    }
}
