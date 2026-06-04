using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleDependencyConfiguration : IEntityTypeConfiguration<ModuleDependency>
{
    public void Configure(EntityTypeBuilder<ModuleDependency> builder)
    {
        builder.HasKey(d => new { d.ModuleId, d.DependsOnModuleId });

        builder.HasOne(d => d.Module)
               .WithMany(m => m.Dependents)
               .HasForeignKey(d => d.ModuleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DependsOnModule)
               .WithMany(m => m.Dependencies)
               .HasForeignKey(d => d.DependsOnModuleId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}