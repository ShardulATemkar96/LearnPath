using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class SubmissionAiFeedbackConfiguration : IEntityTypeConfiguration<SubmissionAiFeedback>
{
    public void Configure(EntityTypeBuilder<SubmissionAiFeedback> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Summary).HasMaxLength(4000);
        builder.Property(f => f.GrammarFeedback).HasMaxLength(4000);
        builder.Property(f => f.RubricCoverage).HasMaxLength(4000);
        builder.Property(f => f.MissingTopics).HasMaxLength(2000);
        builder.Property(f => f.OverallRecommendation).HasMaxLength(2000);
        builder.Property(f => f.Disclaimer).HasMaxLength(500);
        builder.Property(f => f.RawResponse).HasMaxLength(16000);

        builder.HasIndex(f => f.SubmissionId).IsUnique();

        builder.HasOne(f => f.Submission)
               .WithOne()
               .HasForeignKey<SubmissionAiFeedback>(f => f.SubmissionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
