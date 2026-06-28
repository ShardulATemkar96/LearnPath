using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class CommentVoteConfiguration : IEntityTypeConfiguration<CommentVote>
{
    public void Configure(EntityTypeBuilder<CommentVote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.UserId, v.CommentId }).IsUnique();

        builder.HasOne(v => v.User)
               .WithMany()
               .HasForeignKey(v => v.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Comment)
               .WithMany(c => c.Votes)
               .HasForeignKey(v => v.CommentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
