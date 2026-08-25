using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionExtensionLogConfiguration : IEntityTypeConfiguration<AuctionExtensionLog>
{
    public void Configure(EntityTypeBuilder<AuctionExtensionLog> builder)
    {
        builder.ToTable("AuctionExtensionLogs");
        builder.Property(l => l.Reason).HasMaxLength(500).IsRequired();

        builder.HasOne(l => l.TriggeredByBid)
            .WithMany()
            .HasForeignKey(l => l.TriggeredByBidId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
