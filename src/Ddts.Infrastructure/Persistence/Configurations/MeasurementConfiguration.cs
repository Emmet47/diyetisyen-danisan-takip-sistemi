using Ddts.Domain.Clients;
using Ddts.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>measurements tablosu (Şekil 6.1).</summary>
internal sealed class MeasurementConfiguration : IEntityTypeConfiguration<Measurement>
{
    public void Configure(EntityTypeBuilder<Measurement> builder)
    {
        builder.ToTable("measurements", table =>
        {
            table.HasCheckConstraint("ck_measurements_weight", "weight_kg BETWEEN 20 AND 350");
            table.HasCheckConstraint("ck_measurements_height", "height_cm BETWEEN 100 AND 250");
            table.HasCheckConstraint("ck_measurements_waist", "waist_cm BETWEEN 30 AND 250");
            table.HasCheckConstraint("ck_measurements_hip", "hip_cm BETWEEN 30 AND 250");
            table.HasCheckConstraint("ck_measurements_body_fat", "body_fat_percent BETWEEN 2 AND 75");
        });

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<Client>().WithMany().HasForeignKey(m => m.ClientId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(m => m.WeightKg).HasPrecision(5, 1);
        builder.Property(m => m.HeightCm).HasPrecision(4, 1);
        builder.Property(m => m.WaistCm).HasPrecision(4, 1);
        builder.Property(m => m.HipCm).HasPrecision(4, 1);
        builder.Property(m => m.BodyFatPercent).HasPrecision(4, 1);
        builder.Property(m => m.Note).HasMaxLength(300);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);

        // Aynı gün için tek ölçüm (FR-C1)
        builder.HasIndex(m => new { m.ClientId, m.MeasuredOn }).IsUnique();
    }
}
