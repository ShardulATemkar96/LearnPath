using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.ContentUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Feedback).HasMaxLength(2000);

        builder.HasIndex(s => new { s.AssignmentId, s.UserId }).IsUnique();

        builder.HasOne(s => s.Assignment)
               .WithMany(a => a.Submissions)
               .HasForeignKey(s => s.AssignmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
