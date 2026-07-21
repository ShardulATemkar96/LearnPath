using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuestionText).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(q => q.QuestionType)
               .HasConversion<int>()
               .HasDefaultValue(QuizQuestionType.MultipleChoice);
        builder.Property(q => q.Points).HasDefaultValue(1);

        builder.HasOne(q => q.Quiz)
               .WithMany(qz => qz.Questions)
               .HasForeignKey(q => q.QuizId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
