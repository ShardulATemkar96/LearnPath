using LearnPath.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnPath.API.Configurations;

public class ModuleQuizConfiguration : IEntityTypeConfiguration<ModuleQuiz>
{
    public void Configure(EntityTypeBuilder<ModuleQuiz> builder)
    {
        builder.HasKey(mq => mq.Id);
        builder.Property(mq => mq.AssignedBy).HasMaxLength(450).IsRequired();
        builder.Property(mq => mq.Active).HasDefaultValue(true);

        builder.HasOne(mq => mq.Module)
               .WithOne(m => m.ModuleQuiz)
               .HasForeignKey<ModuleQuiz>(mq => mq.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(mq => mq.Quiz)
               .WithMany(q => q.ModuleQuizzes)
               .HasForeignKey(mq => mq.QuizId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(mq => mq.ModuleId).IsUnique();
    }
}
