using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class RfiQuestionOptionConfiguration : IEntityTypeConfiguration<RfiQuestionOption>
{
    public void Configure(EntityTypeBuilder<RfiQuestionOption> builder)
    {
        builder.ToTable("RfiQuestionOptions");
        builder.Property(o => o.OptionText).HasMaxLength(500).IsRequired();
        builder.Property(o => o.ScoreValue).HasPrecision(9, 4);
    }
}
