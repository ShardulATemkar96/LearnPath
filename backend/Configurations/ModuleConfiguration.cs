using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(m => new { m.Title, m.LearningPathId }).IsUnique();
        builder.Property(m => m.ContentType).HasMaxLength(50);
        builder.Property(m => m.Difficulty)
               .HasConversion<int>()
               .HasDefaultValue(ModuleDifficulty.Beginner);
        builder.Property(m => m.NotesHtml).HasColumnType("nvarchar(max)");
        builder.Property(m => m.PdfUrl).HasMaxLength(2048);
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(2048);
        builder.Property(m => m.IsDraft).HasDefaultValue(true);
        builder.Property(m => m.IsPublished).HasDefaultValue(false);
        builder.Property(m => m.IsArchived).HasDefaultValue(false);
        builder.Property(m => m.QuizEnabled).HasDefaultValue(false);
        builder.Property(m => m.QuizQuestionCount).HasDefaultValue(0);
        builder.Property(m => m.QuizPassingScore).HasDefaultValue(0);

        builder.HasOne(m => m.LearningPath)
               .WithMany(p => p.Modules)
               .HasForeignKey(m => m.LearningPathId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}