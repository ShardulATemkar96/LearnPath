using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class PostVoteConfiguration : IEntityTypeConfiguration<PostVote>
{
    public void Configure(EntityTypeBuilder<PostVote> builder)
    {
        builder.HasKey(v => v.Id);
        builder.HasIndex(v => new { v.UserId, v.PostId }).IsUnique();

        builder.HasOne(v => v.User)
               .WithMany()
               .HasForeignKey(v => v.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Post)
               .WithMany(p => p.Votes)
               .HasForeignKey(v => v.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
