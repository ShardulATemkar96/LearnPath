using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.InviteCode).HasMaxLength(8).IsRequired();

        builder.HasIndex(c => c.InviteCode).IsUnique();

        builder.HasOne(c => c.CreatedBy)
               .WithMany()
               .HasForeignKey(c => c.CreatedById)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.LearningPath)
               .WithMany(p => p.Classrooms)
               .HasForeignKey(c => c.LearningPathId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
