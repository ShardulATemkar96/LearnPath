using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Score);
        builder.Property(a => a.Percentage).HasColumnType("decimal(5,2)");
        builder.Property(a => a.Status).HasConversion<int>().HasDefaultValue(AttemptStatus.Created);
        builder.Property(a => a.RandomSeed).HasDefaultValue(0);

        builder.HasOne(a => a.Quiz)
               .WithMany()
               .HasForeignKey(a => a.QuizId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Module)
               .WithMany()
               .HasForeignKey(a => a.ModuleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.QuizId, a.UserId, a.AttemptNumber });
    }
}
