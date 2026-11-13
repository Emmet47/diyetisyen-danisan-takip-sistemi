using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>users tablosu (Şekil 6.1).</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_role", "role IN ('Admin', 'Dietitian', 'Client')");
            table.HasCheckConstraint("ck_users_email_lower", "email = lower(email)");
            table.HasCheckConstraint("ck_users_reset_token", "(reset_token_hash IS NULL) = (reset_token_expires_at IS NULL)");
        });

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasDefaultValueSql(SqlDefaults.NewUuid).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(254);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(200);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(10);
        builder.Property(u => u.IsActive).HasDefaultValue(true).ValueGeneratedNever();

        // Sınıfta private olan alanlar adlarıyla eşlenir (diyagramdaki "-" alanlar).
        builder.Property<string>("ResetTokenHash").HasMaxLength(100).IsRequired(false);
        builder.Property<DateTime?>("ResetTokenExpiresAt");

        builder.Property(u => u.PushToken).HasMaxLength(300);
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql(SqlDefaults.Now);
    }
}
