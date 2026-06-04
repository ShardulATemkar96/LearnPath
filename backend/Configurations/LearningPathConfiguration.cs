using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class LearningPathConfiguration : IEntityTypeConfiguration<LearningPath>
{
    public void Configure(EntityTypeBuilder<LearningPath> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000);

        builder.HasOne(p => p.CreatedBy)
               .WithMany(u => u.CreatedPaths)
               .HasForeignKey(p => p.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);
    }
}