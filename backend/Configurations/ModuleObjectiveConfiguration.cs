using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleObjectiveConfiguration : IEntityTypeConfiguration<ModuleObjective>
{
    public void Configure(EntityTypeBuilder<ModuleObjective> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.ObjectiveText).HasMaxLength(500).IsRequired();

        builder.HasOne(o => o.Module)
               .WithMany(m => m.Objectives)
               .HasForeignKey(o => o.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}