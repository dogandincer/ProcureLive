using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Catalog;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
{
    public void Configure(EntityTypeBuilder<ProductSpecification> builder)
    {
        builder.ToTable("ProductSpecifications");
        builder.Property(s => s.Key).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(1000).IsRequired();
        builder.HasIndex(s => s.ProductId);
    }
}
