using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyEvaluationConfiguration : IEntityTypeConfiguration<CompanyEvaluation>
{
    public void Configure(EntityTypeBuilder<CompanyEvaluation> builder)
    {
        builder.ToTable("CompanyEvaluations");

        builder.Property(e => e.OverallScore).HasPrecision(3, 2);
        builder.Property(e => e.Comments).HasMaxLength(2000);
        builder.Property(e => e.SourceType).HasMaxLength(20);

        builder.HasIndex(e => e.CompanyId);

        // Company relationship is configured from CompanyConfiguration (HasMany -> WithOne).
    }
}
