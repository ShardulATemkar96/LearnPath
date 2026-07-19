using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleTagConfiguration : IEntityTypeConfiguration<ModuleTag>
{
    public void Configure(EntityTypeBuilder<ModuleTag> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TagName).HasMaxLength(100).IsRequired();

        builder.HasOne(t => t.Module)
               .WithMany(m => m.Tags)
               .HasForeignKey(t => t.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}