using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
        builder.Property(m => m.ContentType).HasMaxLength(50);

        builder.HasOne(m => m.LearningPath)
               .WithMany(p => p.Modules)
               .HasForeignKey(m => m.LearningPathId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}