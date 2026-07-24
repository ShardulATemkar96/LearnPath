using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Title).HasMaxLength(200).IsRequired();
        builder.Property(q => q.QuestionCount).HasDefaultValue(0);
        builder.Property(q => q.DifficultyFilter).HasConversion<int?>().IsRequired(false);
        builder.Property(q => q.SelectionMode).HasConversion<int>().HasDefaultValue(SelectionMode.Random);
        builder.Property(q => q.PassingPercentage).HasDefaultValue(40);
        builder.Property(q => q.MaximumAttempts).HasDefaultValue(3);
        builder.Property(q => q.Status).HasConversion<int>().HasDefaultValue(QuizStatus.Draft);

        builder.HasOne(q => q.QuestionBank)
               .WithMany(qb => qb.Quizzes)
               .HasForeignKey(q => q.QuestionBankId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
