using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionAttachmentConfiguration : IEntityTypeConfiguration<AuctionAttachment>
{
    public void Configure(EntityTypeBuilder<AuctionAttachment> builder)
    {
        builder.ToTable("AuctionAttachments");

        builder.Property(a => a.FileName).HasMaxLength(300).IsRequired();
        builder.Property(a => a.Url).HasMaxLength(1000).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(150);

        builder.HasIndex(a => a.AuctionId);

        // Auction relationship configured from AuctionConfiguration (HasMany -> WithOne).
    }
}
