using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Name).HasMaxLength(150).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(1000);

        builder.HasIndex(g => g.OwnerId);
        builder.HasIndex(g => g.CreatedAt);

        builder.HasOne(g => g.Owner)
               .WithMany()
               .HasForeignKey(g => g.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
