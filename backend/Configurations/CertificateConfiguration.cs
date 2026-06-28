using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.UserId, c.LearningPathId }).IsUnique();

        builder.HasOne(c => c.User)
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.LearningPath)
               .WithMany()
               .HasForeignKey(c => c.LearningPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
