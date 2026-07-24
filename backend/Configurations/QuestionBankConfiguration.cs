using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuestionBankConfiguration : IEntityTypeConfiguration<QuestionBank>
{
    public void Configure(EntityTypeBuilder<QuestionBank> builder)
    {
        builder.HasKey(qb => qb.Id);
        builder.Property(qb => qb.Title).HasMaxLength(200).IsRequired();
        builder.Property(qb => qb.Subject).HasMaxLength(100).IsRequired();
        builder.Property(qb => qb.Tags).HasMaxLength(500);
        builder.Property(qb => qb.Version).HasDefaultValue(1);
        builder.Property(qb => qb.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(qb => qb.StoredJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(qb => qb.QuestionCount).HasDefaultValue(0);
        builder.Property(qb => qb.Status).HasConversion<int>().HasDefaultValue(QuestionBankStatus.Draft);
        builder.Property(qb => qb.CreatedBy).HasMaxLength(450).IsRequired();
    }
}
