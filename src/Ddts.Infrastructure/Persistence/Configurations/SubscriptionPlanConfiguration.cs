using Ddts.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>subscription_plans tablosu (Şekil 6.1) ve demo paketleri (FR-L1).</summary>
internal sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    // Sabit kimlikler: migration her ortamda aynı satırları ekler, testler ve web paneli bu kimliklere güvenebilir.
    public static readonly Guid StarterPlanId = new("019a7f00-0000-7000-8000-000000000001");
    public static readonly Guid StandardPlanId = new("019a7f00-0000-7000-8000-000000000002");
    public static readonly Guid ProfessionalPlanId = new("019a7f00-0000-7000-8000-000000000003");

    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("subscription_plans", table =>
        {
            table.HasCheckConstraint("ck_subscription_plans_max_clients", "max_clients > 0");
            table.HasCheckConstraint("ck_subscription_plans_monthly_price", "monthly_price >= 0");
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(50);
        builder.HasIndex(p => p.Name).IsUnique();

        builder.Property(p => p.MonthlyPrice).HasPrecision(8, 2);
        builder.Property(p => p.IsActive).HasDefaultValue(true).ValueGeneratedNever();

        // Demo paketleri (ücretler örnek, ödeme simülasyonla yapılıyor)
        builder.HasData(
            new { Id = StarterPlanId, Name = "Başlangıç", MaxClients = 10, MonthlyPrice = 299m, IsActive = true },
            new { Id = StandardPlanId, Name = "Standart", MaxClients = 30, MonthlyPrice = 599m, IsActive = true },
            new { Id = ProfessionalPlanId, Name = "Profesyonel", MaxClients = 100, MonthlyPrice = 999m, IsActive = true });
    }
}
