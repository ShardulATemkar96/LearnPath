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
        builder.Property(q => q.Description).HasMaxLength(1000);
        builder.Property(q => q.MaxAttempts).HasDefaultValue(0);
        builder.Property(q => q.ShuffleQuestions).HasDefaultValue(false);
        builder.Property(q => q.ShowResults).HasDefaultValue(true);
        builder.Property(q => q.IsMandatory).HasDefaultValue(false);
        builder.Property(q => q.IsPublished).HasDefaultValue(false);

        builder.HasOne(q => q.Module)
               .WithMany(m => m.Quizzes)
               .HasForeignKey(q => q.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => q.ModuleId).IsUnique();
    }
}
