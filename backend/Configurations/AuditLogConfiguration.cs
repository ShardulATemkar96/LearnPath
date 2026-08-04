using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.Timestamp).IsRequired();

        builder.Property(a => a.UserId).HasMaxLength(450);
        builder.Property(a => a.Username).HasMaxLength(256);
        builder.Property(a => a.Role).HasMaxLength(64);

        builder.Property(a => a.ActionType)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(a => a.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(450);
        builder.Property(a => a.Description).HasMaxLength(1000).IsRequired();

        builder.Property(a => a.OldValue).HasMaxLength(4000);
        builder.Property(a => a.NewValue).HasMaxLength(4000);
        builder.Property(a => a.AdditionalData).HasMaxLength(4000);

        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.ActionType);
        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
