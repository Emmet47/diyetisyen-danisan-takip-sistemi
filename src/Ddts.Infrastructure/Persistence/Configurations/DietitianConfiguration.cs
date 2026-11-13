using Ddts.Domain.Dietitians;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>dietitians tablosu (Şekil 6.1).</summary>
internal sealed class DietitianConfiguration : IEntityTypeConfiguration<Dietitian>
{
    public void Configure(EntityTypeBuilder<Dietitian> builder)
    {
        builder.ToTable("dietitians");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        // Her diyetisyenin tek giriş hesabı var; hesap silinirse diyetisyen kaydı da silinir.
        builder.HasOne<User>().WithOne().HasForeignKey<Dietitian>(d => d.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(d => d.UserId).IsUnique();

        builder.Property(d => d.FullName).HasMaxLength(100);
        builder.Property(d => d.Phone).HasMaxLength(20);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);
    }
}
