using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class BidConfiguration : IEntityTypeConfiguration<Bid>
{
    public void Configure(EntityTypeBuilder<Bid> builder)
    {
        builder.ToTable("Bids");

        builder.OwnsOne(b => b.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 4).IsRequired();
            money.Property(m => m.CurrencyId).HasColumnName("CurrencyId").IsRequired();
        });

        builder.Property(b => b.IpAddress).HasMaxLength(45);

        builder.HasIndex(b => new { b.AuctionItemId, b.BidTime });

        builder.HasOne(b => b.Company)
            .WithMany()
            .HasForeignKey(b => b.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
