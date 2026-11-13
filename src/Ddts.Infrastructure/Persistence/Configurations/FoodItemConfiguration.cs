using Ddts.Domain.Dietitians;
using Ddts.Domain.Foods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>food_items tablosu (Şekil 6.2): yerel besin kayıtları.</summary>
internal sealed class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.ToTable("food_items", table =>
        {
            table.HasCheckConstraint("ck_food_items_portion_grams", "portion_grams > 0");
            table.HasCheckConstraint("ck_food_items_energy", "energy_kcal_per_100g BETWEEN 0 AND 900");
            table.HasCheckConstraint("ck_food_items_protein", "protein_g_per_100g BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_food_items_carbs", "carbs_g_per_100g BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_food_items_fat", "fat_g_per_100g BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_food_items_macro_total", "protein_g_per_100g + carbs_g_per_100g + fat_g_per_100g <= 100");
            table.HasCheckConstraint("ck_food_items_external_source", "(external_id IS NULL) = (source_updated_at IS NULL)");
        });

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        // NULL = herkese açık ortak kayıt
        builder.HasOne<Dietitian>().WithMany().HasForeignKey(f => f.DietitianId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(f => f.Name).HasMaxLength(150);
        builder.Property(f => f.PortionName).HasMaxLength(50);
        builder.Property(f => f.PortionGrams).HasPrecision(6, 1);

        // Sütun adları şemadaki gibi (otomatik snake_case "protein_per100g" üretirdi)
        builder.Property(f => f.EnergyKcalPer100g).HasColumnName("energy_kcal_per_100g").HasPrecision(6, 1);
        builder.Property(f => f.ProteinPer100g).HasColumnName("protein_g_per_100g").HasPrecision(5, 1);
        builder.Property(f => f.CarbsPer100g).HasColumnName("carbs_g_per_100g").HasPrecision(5, 1);
        builder.Property(f => f.FatPer100g).HasColumnName("fat_g_per_100g").HasPrecision(5, 1);

        builder.Property(f => f.SourceName).HasMaxLength(50).HasDefaultValue("Yerel").ValueGeneratedNever();
        builder.Property(f => f.ExternalId).HasMaxLength(50);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);

        // Aynı dış besin aynı diyetisyen tarafından iki kez içe aktarılamaz
        builder.HasIndex(f => new { f.DietitianId, f.SourceName, f.ExternalId })
            .IsUnique()
            .HasFilter("external_id IS NOT NULL")
            .HasDatabaseName("ux_food_items_external");
    }
}
