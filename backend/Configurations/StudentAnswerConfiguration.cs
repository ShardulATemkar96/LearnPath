using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class StudentAnswerConfiguration : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Attempt)
               .WithMany(at => at.Answers)
               .HasForeignKey(a => a.QuizAttemptId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
               .WithMany()
               .HasForeignKey(a => a.QuestionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Option)
               .WithMany()
               .HasForeignKey(a => a.OptionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.QuizAttemptId, a.QuestionId }).IsUnique();
    }
}
