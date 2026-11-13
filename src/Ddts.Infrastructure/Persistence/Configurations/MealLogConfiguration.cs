using Ddts.Domain.Clients;
using Ddts.Domain.MealLogs;
using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// meal_logs tablosu (Şekil 6.2). Fotoğraf değer nesnesi (PhotoInfo) aynı tabloda üç sütun;
/// fotoğraf yoksa üçü de boş (CHECK). photo_key'in tekil indeksi ManualSqlConstraints'te.
/// </summary>
internal sealed class MealLogConfiguration : IEntityTypeConfiguration<MealLog>
{
    public void Configure(EntityTypeBuilder<MealLog> builder)
    {
        builder.ToTable("meal_logs", table =>
        {
            table.HasCheckConstraint("ck_meal_logs_status", "status IN ('Eaten', 'Partial', 'Skipped')");
            table.HasCheckConstraint("ck_meal_logs_photo_content_type", "photo_content_type IN ('image/jpeg', 'image/png')");
            table.HasCheckConstraint("ck_meal_logs_photo_size", "photo_size_bytes BETWEEN 1 AND 5242880");
            table.HasCheckConstraint("ck_meal_logs_photo_complete", "num_nonnulls(photo_key, photo_content_type, photo_size_bytes) IN (0, 3)");
        });

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.HasOne<Client>().WithMany().HasForeignKey(l => l.ClientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Meal>().WithMany().HasForeignKey(l => l.MealId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(8);
        builder.Property(l => l.Note).HasMaxLength(500);
        builder.Property(l => l.LoggedAt).HasDefaultValueSql(SqlDefaults.Now);

        // İsteğe bağlı değer nesnesi (EF Core 10): Photo null ise üç sütun da NULL yazılır.
        // ("p!": tip null olabilir ama sütun eşlemesi için derleyiciye "burada null değil" diyoruz)
        builder.ComplexProperty(l => l.Photo, photo =>
        {
            photo.IsRequired(false);
            photo.Property(p => p!.Key).HasColumnName("photo_key").HasMaxLength(200);
            photo.Property(p => p!.ContentType).HasColumnName("photo_content_type").HasMaxLength(10);
            photo.Property(p => p!.SizeBytes).HasColumnName("photo_size_bytes");
        });

        // Bir öğün aynı gün için bir kez kaydedilir (FR-G1)
        builder.HasIndex(l => new { l.ClientId, l.MealId, l.LogDate }).IsUnique();
        builder.HasIndex(l => new { l.ClientId, l.LogDate }).HasDatabaseName("ix_meal_logs_client_date");
    }
}
