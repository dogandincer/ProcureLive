using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.Property(c => c.Name).HasMaxLength(250).IsRequired();
        builder.Property(c => c.TradeName).HasMaxLength(300);
        builder.Property(c => c.TaxNumber).HasMaxLength(11).IsRequired();
        builder.Property(c => c.TaxOffice).HasMaxLength(150).IsRequired();
        builder.Property(c => c.MersisNo).HasMaxLength(20);
        builder.Property(c => c.Website).HasMaxLength(250);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(2000);

        builder.HasIndex(c => c.TaxNumber).IsUnique();

        builder.HasMany(c => c.Evaluations)
            .WithOne(e => e.Company)
            .HasForeignKey(e => e.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.ContactPersons)
            .WithOne(cp => cp.Company)
            .HasForeignKey(cp => cp.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Addresses)
            .WithOne(a => a.Company)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Workspace)
            .WithMany()
            .HasForeignKey(c => c.WorkspaceId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
