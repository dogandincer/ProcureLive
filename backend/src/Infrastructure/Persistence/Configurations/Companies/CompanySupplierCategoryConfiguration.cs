using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanySupplierCategoryConfiguration : IEntityTypeConfiguration<CompanySupplierCategory>
{
    public void Configure(EntityTypeBuilder<CompanySupplierCategory> builder)
    {
        builder.ToTable("CompanySupplierCategories");

        builder.HasIndex(x => new { x.CompanyId, x.CategoryId }).IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany(c => c.SupplierCategories)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
