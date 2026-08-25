using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class RfiResponseConfiguration : IEntityTypeConfiguration<RfiResponse>
{
    public void Configure(EntityTypeBuilder<RfiResponse> builder)
    {
        builder.ToTable("RfiResponses");
        builder.Property(r => r.AnswerText).HasMaxLength(4000);
        builder.Property(r => r.AnswerNumber).HasPrecision(18, 4);
        builder.Property(r => r.AttachmentUrl).HasMaxLength(1000);

        builder.HasOne(r => r.RfiQuestion)
            .WithMany()
            .HasForeignKey(r => r.RfiQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.SelectedOption)
            .WithMany()
            .HasForeignKey(r => r.SelectedOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
