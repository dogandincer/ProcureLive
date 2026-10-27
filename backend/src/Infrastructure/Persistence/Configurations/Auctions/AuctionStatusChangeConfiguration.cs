using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionStatusChangeConfiguration : IEntityTypeConfiguration<AuctionStatusChange>
{
    public void Configure(EntityTypeBuilder<AuctionStatusChange> builder)
    {
        builder.ToTable("AuctionStatusChanges");

        builder.Property(c => c.Reason).HasMaxLength(500);

        builder.HasIndex(c => new { c.AuctionId, c.ChangedAt });

        builder.HasOne(c => c.Auction)
            .WithMany(a => a.StatusChanges)
            .HasForeignKey(c => c.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
