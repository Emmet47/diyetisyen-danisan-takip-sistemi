using Ddts.Domain.Clients;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>clients tablosu (Şekil 6.1).</summary>
internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients", table =>
        {
            table.HasCheckConstraint("ck_clients_sex", "sex IN ('Female', 'Male')");
            table.HasCheckConstraint("ck_clients_goal_type", "goal_type IN ('LoseWeight', 'Maintain', 'GainWeight')");
            table.HasCheckConstraint("ck_clients_target_weight", "target_weight_kg BETWEEN 20 AND 350");
        });

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        // Tenant: danışan tek diyetisyene bağlı. Diyetisyen silinmeden önce danışanları silinmeli (NO ACTION).
        builder.HasOne<Dietitian>().WithMany().HasForeignKey(c => c.DietitianId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(c => c.DietitianId).HasDatabaseName("ix_clients_dietitian");

        builder.HasOne<User>().WithOne().HasForeignKey<Client>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => c.UserId).IsUnique();

        // (id, dietitian_id) tekil: plan ve randevu tablolarındaki bileşik FK bu anahtarı gösterir,
        // böylece başka diyetisyenin danışanına plan/randevu yazılamaz (tenant tutarlılığı).
        builder.HasAlternateKey(c => new { c.Id, c.DietitianId });

        builder.Property(c => c.FullName).HasMaxLength(100);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.Sex).HasConversion<string>().HasMaxLength(6);
        builder.Property(c => c.GoalType).HasConversion<string>().HasMaxLength(10);
        builder.Property(c => c.TargetWeightKg).HasPrecision(5, 1);
        builder.Property(c => c.Preferences).HasMaxLength(500);
        builder.Property(c => c.AllergyNotes).HasMaxLength(500);
        builder.Property(c => c.IsActive).HasDefaultValue(true).ValueGeneratedNever();

        // Bildirim izinleri değer nesnesi: clients tablosunda dört sütun (FR-J2)
        builder.ComplexProperty(c => c.Notifications, notifications =>
        {
            notifications.Property(n => n.Appointments)
                .HasColumnName("notify_appointments").HasDefaultValue(true).ValueGeneratedNever();
            notifications.Property(n => n.MealReminders)
                .HasColumnName("notify_meal_reminders").HasDefaultValue(true).ValueGeneratedNever();
            notifications.Property(n => n.Measurements)
                .HasColumnName("notify_measurements").HasDefaultValue(true).ValueGeneratedNever();
            notifications.Property(n => n.Plans)
                .HasColumnName("notify_plans").HasDefaultValue(true).ValueGeneratedNever();
        });

        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);
    }
}
