using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuizOptionConfiguration : IEntityTypeConfiguration<QuizOption>
{
    public void Configure(EntityTypeBuilder<QuizOption> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OptionText).HasMaxLength(500).IsRequired();
        builder.Property(o => o.IsCorrect).HasDefaultValue(false);

        builder.HasOne(o => o.Question)
               .WithMany(q => q.Options)
               .HasForeignKey(o => o.QuizQuestionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
