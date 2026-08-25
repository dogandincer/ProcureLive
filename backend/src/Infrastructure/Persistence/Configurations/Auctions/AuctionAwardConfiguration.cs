using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionAwardConfiguration : IEntityTypeConfiguration<AuctionAward>
{
    public void Configure(EntityTypeBuilder<AuctionAward> builder)
    {
        builder.ToTable("AuctionAwards");
        builder.Property(a => a.Notes).HasMaxLength(1000);

        builder.HasIndex(a => new { a.AuctionId, a.AuctionItemId }).IsUnique();

        builder.HasOne(a => a.Auction)
            .WithMany()
            .HasForeignKey(a => a.AuctionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AuctionItem)
            .WithMany()
            .HasForeignKey(a => a.AuctionItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.WinningBid)
            .WithMany()
            .HasForeignKey(a => a.WinningBidId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AwardedCompany)
            .WithMany()
            .HasForeignKey(a => a.AwardedCompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
