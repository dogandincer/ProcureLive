using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanySectorConfiguration : IEntityTypeConfiguration<CompanySector>
{
    public void Configure(EntityTypeBuilder<CompanySector> builder)
    {
        builder.ToTable("CompanySectors");

        builder.HasIndex(x => new { x.CompanyId, x.SectorId }).IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany(c => c.Sectors)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sector)
            .WithMany(s => s.Companies)
            .HasForeignKey(x => x.SectorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
