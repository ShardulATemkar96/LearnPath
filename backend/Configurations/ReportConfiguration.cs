using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TargetType).HasMaxLength(20).IsRequired();
        builder.Property(r => r.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(r => r.Status).HasMaxLength(20).HasDefaultValue("Pending");

        builder.HasIndex(r => new { r.TargetType, r.TargetId });
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => r.ReportedByUserId);

        builder.HasOne(r => r.ReportedBy)
               .WithMany()
               .HasForeignKey(r => r.ReportedByUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
