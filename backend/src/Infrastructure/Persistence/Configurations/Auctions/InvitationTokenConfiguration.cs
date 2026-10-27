using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class InvitationTokenConfiguration : IEntityTypeConfiguration<InvitationToken>
{
    public void Configure(EntityTypeBuilder<InvitationToken> builder)
    {
        builder.ToTable("InvitationTokens");

        builder.Property(t => t.Token).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Email).HasMaxLength(200);

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.AuctionId);

        builder.HasOne(t => t.Auction)
            .WithMany()
            .HasForeignKey(t => t.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Company)
            .WithMany()
            .HasForeignKey(t => t.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
