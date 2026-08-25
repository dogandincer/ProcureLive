using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class RfiQuestionConfiguration : IEntityTypeConfiguration<RfiQuestion>
{
    public void Configure(EntityTypeBuilder<RfiQuestion> builder)
    {
        builder.ToTable("RfiQuestions");
        builder.Property(q => q.QuestionText).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.Weight).HasPrecision(9, 4);

        builder.HasMany(q => q.Options)
            .WithOne(o => o.RfiQuestion)
            .HasForeignKey(o => o.RfiQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
