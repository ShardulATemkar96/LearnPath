using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status)
               .HasConversion<int>()
               .HasDefaultValue(AttemptStatus.InProgress);

        builder.HasOne(a => a.Quiz)
               .WithMany(q => q.Attempts)
               .HasForeignKey(a => a.QuizId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.QuizId, a.UserId })
               .HasFilter("[Status] = 0")
               .HasDatabaseName("IX_QuizAttempt_OneActivePerUser");
    }
}
