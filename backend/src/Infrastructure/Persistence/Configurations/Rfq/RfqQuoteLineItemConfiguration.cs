using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfq;

public sealed class RfqQuoteLineItemConfiguration : IEntityTypeConfiguration<RfqQuoteLineItem>
{
    public void Configure(EntityTypeBuilder<RfqQuoteLineItem> builder)
    {
        builder.ToTable("RfqQuoteLineItems");

        builder.OwnsOne(l => l.UnitPrice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("UnitPriceAmount").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.CurrencyId).HasColumnName("UnitPriceCurrencyId").IsRequired();
        });

        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.HasIndex(l => new { l.RfqQuoteId, l.RfqLineItemId }).IsUnique();
    }
}
