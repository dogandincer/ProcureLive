using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class ParticipationRequestConfiguration : IEntityTypeConfiguration<ParticipationRequest>
{
    public void Configure(EntityTypeBuilder<ParticipationRequest> builder)
    {
        builder.ToTable("ParticipationRequests");

        builder.Property(r => r.Note).HasMaxLength(1000);
        builder.Property(r => r.RejectReason).HasMaxLength(500);

        builder.HasIndex(r => new { r.AuctionId, r.CompanyId });
        builder.HasIndex(r => r.Status);

        builder.HasOne(r => r.Auction)
            .WithMany()
            .HasForeignKey(r => r.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.ParticipationRequest)
            .HasForeignKey(l => l.ParticipationRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ParticipationRequestLineConfiguration : IEntityTypeConfiguration<ParticipationRequestLine>
{
    public void Configure(EntityTypeBuilder<ParticipationRequestLine> builder)
    {
        builder.ToTable("ParticipationRequestLines");

        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.Note).HasMaxLength(500);

        builder.HasIndex(l => new { l.ParticipationRequestId, l.AuctionItemId }).IsUnique();

        builder.HasOne(l => l.AuctionItem)
            .WithMany()
            .HasForeignKey(l => l.AuctionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
