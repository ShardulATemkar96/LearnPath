using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleDependencyConfiguration : IEntityTypeConfiguration<ModuleDependency>
{
    public void Configure(EntityTypeBuilder<ModuleDependency> builder)
    {
        builder.HasKey(d => new { d.ModuleId, d.DependsOnModuleId });

        // Navigation semantics (keep aligned with every consumer that reads
        // Module.Dependencies as "modules this module depends on"):
        // - Module.Dependencies  -> edges where d.ModuleId == m.Id
        //   (m depends on d.DependsOnModule).
        // - Module.Dependents    -> edges where d.DependsOnModuleId == m.Id
        //   (m is the prerequisite of d.Module).
        // Names only: this has no database schema impact (FKs are unchanged).
        builder.HasOne(d => d.Module)
               .WithMany(m => m.Dependencies)
               .HasForeignKey(d => d.ModuleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DependsOnModule)
               .WithMany(m => m.Dependents)
               .HasForeignKey(d => d.DependsOnModuleId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}