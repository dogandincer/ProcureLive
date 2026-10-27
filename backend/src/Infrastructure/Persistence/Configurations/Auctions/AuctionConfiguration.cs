using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Auctions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Auctions;

public sealed class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("Auctions");

        builder.Property(a => a.Title).HasMaxLength(250).IsRequired();
        builder.Property(a => a.ReferenceCode).HasMaxLength(50);
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.Property(a => a.ReservePrice).HasPrecision(18, 4);
        builder.Property(a => a.MinDecrementAmount).HasPrecision(18, 4);
        builder.Property(a => a.MinDecrementPercentage).HasPrecision(9, 4);

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.EndAt);
        builder.HasIndex(a => a.ReferenceCode);

        builder.HasOne(a => a.OwnerCompany)
            .WithMany()
            .HasForeignKey(a => a.OwnerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.WorkspaceId);

        builder.HasOne<ProcureLive.Domain.Entities.Workspaces.Workspace>()
            .WithMany()
            .HasForeignKey(a => a.WorkspaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Currency)
            .WithMany()
            .HasForeignKey(a => a.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(a => a.Items)
            .WithOne(i => i.Auction)
            .HasForeignKey(i => i.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Invitations)
            .WithOne(i => i.Auction)
            .HasForeignKey(i => i.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.ExtensionLogs)
            .WithOne(l => l.Auction)
            .HasForeignKey(l => l.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Attachments)
            .WithOne(x => x.Auction)
            .HasForeignKey(x => x.AuctionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
