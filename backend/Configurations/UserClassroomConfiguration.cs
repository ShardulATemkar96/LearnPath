using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class UserClassroomConfiguration : IEntityTypeConfiguration<UserClassroom>
{
    public void Configure(EntityTypeBuilder<UserClassroom> builder)
    {
        builder.HasKey(uc => new { uc.UserId, uc.ClassroomId });

        builder.HasOne(uc => uc.User)
               .WithMany(u => u.UserClassrooms)
               .HasForeignKey(uc => uc.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(uc => uc.Classroom)
               .WithMany(c => c.UserClassrooms)
               .HasForeignKey(uc => uc.ClassroomId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(uc => uc.Role).HasMaxLength(50);
    }
}