using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfq;

public sealed class RfqProcessConfiguration : IEntityTypeConfiguration<RfqProcess>
{
    public void Configure(EntityTypeBuilder<RfqProcess> builder)
    {
        builder.ToTable("RfqProcesses");
        builder.Property(p => p.Title).HasMaxLength(250).IsRequired();

        builder.HasOne(p => p.OwnerCompany)
            .WithMany()
            .HasForeignKey(p => p.OwnerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Currency)
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.LineItems)
            .WithOne(l => l.RfqProcess)
            .HasForeignKey(l => l.RfqProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Invitations)
            .WithOne(i => i.RfqProcess)
            .HasForeignKey(i => i.RfqProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Quotes)
            .WithOne(q => q.RfqProcess)
            .HasForeignKey(q => q.RfqProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RfqQuote>()
            .WithMany()
            .HasForeignKey(p => p.WinningQuoteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
