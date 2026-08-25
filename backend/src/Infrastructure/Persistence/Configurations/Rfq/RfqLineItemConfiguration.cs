using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfq;

public sealed class RfqLineItemConfiguration : IEntityTypeConfiguration<RfqLineItem>
{
    public void Configure(EntityTypeBuilder<RfqLineItem> builder)
    {
        builder.ToTable("RfqLineItems");
        builder.Property(l => l.Description).HasMaxLength(1000).IsRequired();
        builder.Property(l => l.Quantity).HasPrecision(18, 3);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(l => l.QuoteLineItems)
            .WithOne(q => q.RfqLineItem)
            .HasForeignKey(q => q.RfqLineItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
