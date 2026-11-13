using Ddts.Domain.Dietitians;
using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>plan_templates tablosu (Şekil 6.2). Öğünleri meals tablosunda (template_id dolu).</summary>
internal sealed class PlanTemplateConfiguration : IEntityTypeConfiguration<PlanTemplate>
{
    public void Configure(EntityTypeBuilder<PlanTemplate> builder)
    {
        builder.ToTable("plan_templates", table =>
            table.HasCheckConstraint("ck_plan_templates_plan_type", "plan_type IN ('Daily', 'Weekly')"));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<Dietitian>().WithMany().HasForeignKey(t => t.DietitianId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.Name).HasMaxLength(100);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.PlanType).HasConversion<string>().HasMaxLength(6);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql(SqlDefaults.Now);
    }
}
