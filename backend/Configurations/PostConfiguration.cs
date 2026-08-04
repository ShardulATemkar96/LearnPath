using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Title).HasMaxLength(300).IsRequired();
        builder.Property(p => p.Content).HasMaxLength(10000).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100);
        builder.Property(p => p.Score).HasDefaultValue(0);

        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.AuthorId);
        builder.HasIndex(p => p.GroupId);
        builder.HasIndex(p => p.Score);
        builder.HasIndex(p => new { p.IsPinned, p.CreatedAt });

        builder.HasOne(p => p.Author)
               .WithMany()
               .HasForeignKey(p => p.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.LearningPath)
               .WithMany()
               .HasForeignKey(p => p.LearningPathId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);

        builder.HasOne(p => p.Group)
               .WithMany(g => g.Posts)
               .HasForeignKey(p => p.GroupId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}
