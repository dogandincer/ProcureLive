using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionItemConfiguration : IEntityTypeConfiguration<AuctionItem>
{
    public void Configure(EntityTypeBuilder<AuctionItem> builder)
    {
        builder.ToTable("AuctionItems");

        builder.Property(i => i.Description).HasMaxLength(1000).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.TargetPrice).HasPrecision(18, 4);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Bids)
            .WithOne(b => b.AuctionItem)
            .HasForeignKey(b => b.AuctionItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
