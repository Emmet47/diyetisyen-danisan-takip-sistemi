using Ddts.Domain.Clients;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>nutrition_plans tablosu (Şekil 6.2). Öğünleri meals tablosunda (plan_id dolu).</summary>
internal sealed class NutritionPlanConfiguration : IEntityTypeConfiguration<NutritionPlan>
{
    public void Configure(EntityTypeBuilder<NutritionPlan> builder)
    {
        builder.ToTable("nutrition_plans", table =>
        {
            table.HasCheckConstraint("ck_nutrition_plans_plan_type", "plan_type IN ('Daily', 'Weekly')");
            table.HasCheckConstraint("ck_nutrition_plans_status", "status IN ('Draft', 'Published', 'Archived')");
            table.HasCheckConstraint("ck_nutrition_plans_target_kcal", "target_kcal BETWEEN 800 AND 6000");
            // Durum ile tarih alanları tutarlı olmalı: Taslak -> Yayımlandı -> Arşiv (BR-04)
            table.HasCheckConstraint(
                "ck_nutrition_plans_status_dates",
                """
                (status = 'Draft'     AND published_at IS NULL     AND archived_at IS NULL) OR
                (status = 'Published' AND published_at IS NOT NULL AND archived_at IS NULL) OR
                (status = 'Archived'  AND published_at IS NOT NULL AND archived_at IS NOT NULL)
                """);
        });

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<Dietitian>().WithMany().HasForeignKey(p => p.DietitianId).OnDelete(DeleteBehavior.NoAction);

        // Bileşik FK: plan ile danışan aynı diyetisyene ait olmak zorunda (tenant tutarlılığı)
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(p => new { p.ClientId, p.DietitianId })
            .HasPrincipalKey(c => new { c.Id, c.DietitianId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PlanTemplate>().WithMany().HasForeignKey(p => p.SourceTemplateId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.Title).HasMaxLength(100);
        builder.Property(p => p.PlanType).HasConversion<string>().HasMaxLength(6);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(10)
            .HasDefaultValueSql("'Draft'").ValueGeneratedNever();
        builder.Property(p => p.EnergyFormula).HasMaxLength(30);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql(SqlDefaults.Now);

        builder.Ignore(p => p.DomainEvents);

        // Bir danışanın aynı anda tek yayımlanmış planı olur (BR-03)
        builder.HasIndex(p => p.ClientId)
            .IsUnique()
            .HasFilter("status = 'Published'")
            .HasDatabaseName("ux_nutrition_plans_one_published");
        builder.HasIndex(p => p.DietitianId).HasDatabaseName("ix_nutrition_plans_dietitian");
    }
}
