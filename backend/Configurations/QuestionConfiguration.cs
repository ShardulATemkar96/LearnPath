using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuestionText).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(q => q.Difficulty).HasConversion<int>().HasDefaultValue(Difficulty.Easy);
        builder.Property(q => q.Explanation).HasColumnType("nvarchar(max)");

        builder.HasOne(q => q.QuestionBank)
               .WithMany(qb => qb.Questions)
               .HasForeignKey(q => q.QuestionBankId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
