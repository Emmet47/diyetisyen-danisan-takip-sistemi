using Ddts.Domain.Dietitians;
using Ddts.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>subscriptions tablosu (Şekil 6.1).</summary>
internal sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions", table =>
        {
            table.HasCheckConstraint("ck_subscriptions_status", "status IN ('Active', 'Expired', 'Cancelled')");
            table.HasCheckConstraint("ck_subscriptions_dates", "start_date < end_date");
        });

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<Dietitian>().WithMany().HasForeignKey(s => s.DietitianId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.SubscriptionPlanId).OnDelete(DeleteBehavior.NoAction);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);

        // Bir diyetisyenin aynı anda yalnızca bir aktif aboneliği olabilir (kısmi tekil indeks)
        builder.HasIndex(s => s.DietitianId)
            .IsUnique()
            .HasFilter("status = 'Active'")
            .HasDatabaseName("ux_subscriptions_one_active");
    }
}
