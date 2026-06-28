using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Content).HasMaxLength(5000).IsRequired();

        builder.HasOne(c => c.Author)
               .WithMany()
               .HasForeignKey(c => c.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Post)
               .WithMany(p => p.Comments)
               .HasForeignKey(c => c.PostId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.ParentComment)
               .WithMany(c => c.Replies)
               .HasForeignKey(c => c.ParentCommentId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
