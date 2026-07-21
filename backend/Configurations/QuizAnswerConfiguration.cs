using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuizAnswerConfiguration : IEntityTypeConfiguration<QuizAnswer>
{
    public void Configure(EntityTypeBuilder<QuizAnswer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Attempt)
               .WithMany(at => at.Answers)
               .HasForeignKey(a => a.QuizAttemptId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
               .WithMany(q => q.Answers)
               .HasForeignKey(a => a.QuizQuestionId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SelectedOption)
               .WithMany()
               .HasForeignKey(a => a.SelectedOptionId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
