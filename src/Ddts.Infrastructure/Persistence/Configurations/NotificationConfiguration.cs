using Ddts.Domain.Notifications;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>notifications tablosu (Şekil 6.1).</summary>
internal sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications", table =>
            table.HasCheckConstraint(
                "ck_notifications_type",
                """
                type IN (
                    'AppointmentRequested', 'AppointmentConfirmed', 'AppointmentCancelled',
                    'AppointmentRescheduled', 'AppointmentReminder',
                    'PlanPublished', 'MealLogReminder', 'MeasurementReminder')
                """));

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(n => n.Title).HasMaxLength(100);
        builder.Property(n => n.Body).HasMaxLength(300);
        builder.Property(n => n.IsRead).HasDefaultValue(false).ValueGeneratedNever();
        builder.Property(n => n.CreatedAt).HasDefaultValueSql(SqlDefaults.Now);

        // Aynı hatırlatma iki kez üretilmesin (ör. appt:{id}:24h), BR-12
        builder.Property(n => n.DedupKey).HasMaxLength(100);
        builder.HasIndex(n => n.DedupKey).IsUnique();

        // Kullanıcının bildirim listesi: en yeni üstte
        builder.HasIndex(n => new { n.UserId, n.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_notifications_user");
    }
}
