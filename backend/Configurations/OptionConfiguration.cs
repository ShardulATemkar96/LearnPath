using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class OptionConfiguration : IEntityTypeConfiguration<Option>
{
    public void Configure(EntityTypeBuilder<Option> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OptionText).HasMaxLength(500).IsRequired();
        builder.Property(o => o.IsCorrect).HasDefaultValue(false);
        builder.Property(o => o.DisplayOrder).HasDefaultValue(0);

        builder.HasOne(o => o.Question)
               .WithMany(q => q.Options)
               .HasForeignKey(o => o.QuestionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.QuestionId, o.DisplayOrder });
    }
}
