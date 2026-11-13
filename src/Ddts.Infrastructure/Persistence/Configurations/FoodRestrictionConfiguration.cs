using Ddts.Domain.Clients;
using Ddts.Domain.Foods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>
/// client_food_restrictions tablosu (Şekil 6.2). Birincil anahtar (client_id, food_item_id);
/// client_id sınıfta yok, gölge özellik olarak tutulur.
/// </summary>
internal sealed class FoodRestrictionConfiguration : IEntityTypeConfiguration<FoodRestriction>
{
    public void Configure(EntityTypeBuilder<FoodRestriction> builder)
    {
        builder.ToTable("client_food_restrictions", table =>
            table.HasCheckConstraint("ck_client_food_restrictions_type", "type IN ('Allergy', 'Dislike')"));

        builder.Property<Guid>("ClientId");
        builder.HasKey("ClientId", nameof(FoodRestriction.FoodItemId));

        builder.HasOne<Client>()
            .WithMany(c => c.Restrictions)
            .HasForeignKey("ClientId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FoodItem>().WithMany().HasForeignKey(r => r.FoodItemId).OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(10);
    }
}
