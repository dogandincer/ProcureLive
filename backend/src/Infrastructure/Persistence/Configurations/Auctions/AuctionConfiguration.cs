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
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.Property(a => a.ReservePrice).HasPrecision(18, 4);
        builder.Property(a => a.MinDecrementAmount).HasPrecision(18, 4);
        builder.Property(a => a.MinDecrementPercentage).HasPrecision(9, 4);

        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.EndAt);

        builder.HasOne(a => a.OwnerCompany)
            .WithMany()
            .HasForeignKey(a => a.OwnerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

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
    }
}
