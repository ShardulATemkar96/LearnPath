using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleResourceConfiguration : IEntityTypeConfiguration<ModuleResource>
{
    public void Configure(EntityTypeBuilder<ModuleResource> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Type).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Url).HasMaxLength(2048).IsRequired();

        builder.HasOne(r => r.Module)
               .WithMany(m => m.Resources)
               .HasForeignKey(r => r.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}