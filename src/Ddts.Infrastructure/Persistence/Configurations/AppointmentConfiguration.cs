using Ddts.Domain.Appointments;
using Ddts.Domain.Clients;
using Ddts.Domain.Dietitians;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// appointments tablosu (Şekil 6.1). Çift randevuyu önleyen EXCLUDE kısıtı EF Core modelinde
/// tanımlanamadığı için ManualSqlConstraints ile migration'a eklenir (ADR-0002).
/// </summary>
internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments", table =>
        {
            table.HasCheckConstraint("ck_appointments_status", "status IN ('Requested', 'Confirmed', 'Cancelled', 'Completed')");
            table.HasCheckConstraint("ck_appointments_time_range", "starts_at < ends_at");
        });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<Dietitian>().WithMany().HasForeignKey(a => a.DietitianId).OnDelete(DeleteBehavior.NoAction);

        // Bileşik FK: randevudaki danışan, randevunun diyetisyenine bağlı olmak zorunda
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(a => new { a.ClientId, a.DietitianId })
            .HasPrincipalKey(c => new { c.Id, c.DietitianId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(10);
        builder.Property(a => a.Note).HasMaxLength(300);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);

        builder.Ignore(a => a.DomainEvents);

        builder.HasIndex(a => a.ClientId).HasDatabaseName("ix_appointments_client");
    }
}
