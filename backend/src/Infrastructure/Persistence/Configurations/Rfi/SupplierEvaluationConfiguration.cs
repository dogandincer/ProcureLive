using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class SupplierEvaluationConfiguration : IEntityTypeConfiguration<SupplierEvaluation>
{
    public void Configure(EntityTypeBuilder<SupplierEvaluation> builder)
    {
        builder.ToTable("SupplierEvaluations");
        builder.Property(e => e.TotalScore).HasPrecision(9, 4);
        builder.Property(e => e.Comments).HasMaxLength(2000);

        builder.HasOne(e => e.Company)
            .WithMany()
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
