using Ddts.Domain.Auditing;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ddts.Infrastructure.Persistence.Configurations;

/// <summary>audit_logs tablosu (Şekil 6.1): denetim izi.</summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        // bigint GENERATED ALWAYS AS IDENTITY: değeri yalnızca veritabanı verir
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).UseIdentityAlwaysColumn();

        // Kullanıcı silinse de kayıt kalır, işlemi yapan alanı boşalır
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(a => a.Action).HasMaxLength(50);
        builder.Property(a => a.EntityType).HasMaxLength(50);
        builder.Property(a => a.Details).HasColumnType("jsonb");
        builder.Property(a => a.IpAddress).HasMaxLength(45);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql(SqlDefaults.Now);

        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt }).HasDatabaseName("ix_audit_logs_entity");
    }
}
