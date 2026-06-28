using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000);

        builder.HasOne(a => a.Classroom)
               .WithMany(c => c.Assignments)
               .HasForeignKey(a => a.ClassroomId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
