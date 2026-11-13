using Ddts.Domain.Dietitians;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// working_hours tablosu (Şekil 6.1). Sınıfta kimlik ve diyetisyen alanı yok (diyagramda da yok);
/// ikisi de gölge özellik (shadow property) olarak yalnızca veritabanı tarafında tutulur.
/// </summary>
internal sealed class WorkingHourConfiguration : IEntityTypeConfiguration<WorkingHour>
{
    public void Configure(EntityTypeBuilder<WorkingHour> builder)
    {
        builder.ToTable("working_hours", table =>
        {
            table.HasCheckConstraint("ck_working_hours_weekday", "weekday BETWEEN 1 AND 7");
            table.HasCheckConstraint("ck_working_hours_time_range", "start_time < end_time");
        });

        builder.Property<Guid>("Id").HasDefaultValueSql(SqlDefaults.NewUuid);
        builder.HasKey("Id");

        builder.Property<Guid>("DietitianId");
        builder.HasOne<Dietitian>()
            .WithMany(d => d.WorkingHours)
            .HasForeignKey("DietitianId")
            .OnDelete(DeleteBehavior.Cascade);

        // Sınıfta int, veritabanında smallint
        builder.Property(w => w.Weekday).HasConversion<short>();

        // Her gün için tek aralık
        builder.HasIndex("DietitianId", nameof(WorkingHour.Weekday)).IsUnique();
    }
}
