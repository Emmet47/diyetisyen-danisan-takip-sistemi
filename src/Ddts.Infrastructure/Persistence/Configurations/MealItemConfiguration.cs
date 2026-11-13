using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// meal_items tablosu (Şekil 6.2): Composite'in yaprağı. Alternatif satır, bileşik FK
/// (alternative_to_item_id, meal_id) ile aynı öğündeki ana satırı gösterir; başka öğündeki
/// satıra bağlanması veritabanında da engellenir (BR-06).
/// </summary>
internal sealed class MealItemConfiguration : IEntityTypeConfiguration<MealItem>
{
    public void Configure(EntityTypeBuilder<MealItem> builder)
    {
        builder.ToTable("meal_items", table =>
        {
            table.HasCheckConstraint("ck_meal_items_portion_count", "portion_count > 0 AND portion_count <= 20");
            table.HasCheckConstraint("ck_meal_items_not_self", "alternative_to_item_id IS NULL OR alternative_to_item_id <> id");
        });

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.Property<Guid>("MealId");
        builder.HasOne<Meal>()
            .WithMany(m => m.Items)
            .HasForeignKey("MealId")
            .OnDelete(DeleteBehavior.Cascade);

        // Planda ya da şablonda kullanılan besin silinemez (FR-D2)
        builder.HasOne(i => i.FoodItem).WithMany().HasForeignKey(i => i.FoodItemId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(i => i.PortionCount).HasPrecision(5, 2);
        builder.Ignore(i => i.IsAlternative);

        // (id, meal_id) tekil: alternatif satırın bileşik FK'si bu anahtarı gösterir
        builder.HasAlternateKey("Id", "MealId");
        builder.HasOne<MealItem>()
            .WithMany()
            .HasForeignKey(nameof(MealItem.AlternativeToItemId), "MealId")
            .HasPrincipalKey("Id", "MealId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("MealId").HasDatabaseName("ix_meal_items_meal");
    }
}
