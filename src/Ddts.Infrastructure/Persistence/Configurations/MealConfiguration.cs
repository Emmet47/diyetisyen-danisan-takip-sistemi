using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// meals tablosu (Şekil 6.2). Öğün ya bir plana ya da bir şablona aittir: plan_id ve template_id
/// gölge özellik, ikisinden tam olarak biri dolu olmalı (CHECK).
/// </summary>
internal sealed class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.ToTable("meals", table =>
        {
            table.HasCheckConstraint("ck_meals_day_no", "day_no BETWEEN 1 AND 7");
            table.HasCheckConstraint("ck_meals_owner", "num_nonnulls(plan_id, template_id) = 1");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.Property<Guid?>("PlanId");
        builder.HasOne<NutritionPlan>()
            .WithMany(p => p.Meals)
            .HasForeignKey("PlanId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property<Guid?>("TemplateId");
        builder.HasOne<PlanTemplate>()
            .WithMany(t => t.Meals)
            .HasForeignKey("TemplateId")
            .OnDelete(DeleteBehavior.Cascade);

        // Sınıfta int, veritabanında smallint (günlük planda hep 1)
        builder.Property(m => m.DayNo).HasConversion<short>();
        builder.Property(m => m.Name).HasMaxLength(50);
        builder.Property(m => m.Time).HasColumnName("meal_time");

        builder.HasIndex("PlanId").HasDatabaseName("ix_meals_plan");
        builder.HasIndex("TemplateId").HasDatabaseName("ix_meals_template");
    }
}
