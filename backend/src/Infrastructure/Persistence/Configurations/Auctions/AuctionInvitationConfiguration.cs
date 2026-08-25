using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionInvitationConfiguration : IEntityTypeConfiguration<AuctionInvitation>
{
    public void Configure(EntityTypeBuilder<AuctionInvitation> builder)
    {
        builder.ToTable("AuctionInvitations");

        builder.HasIndex(i => new { i.AuctionId, i.CompanyId }).IsUnique();

        builder.HasOne(i => i.Company)
            .WithMany()
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
