using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ProgressConfiguration : IEntityTypeConfiguration<Progress>
{
    public void Configure(EntityTypeBuilder<Progress> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => new { p.UserId, p.ModuleId }).IsUnique();

        builder.HasOne(p => p.User)
               .WithMany(u => u.Progresses)
               .HasForeignKey(p => p.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Module)
               .WithMany(m => m.Progresses)
               .HasForeignKey(p => p.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
