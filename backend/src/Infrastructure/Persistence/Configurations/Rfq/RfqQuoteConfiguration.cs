using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfq;

public sealed class RfqQuoteConfiguration : IEntityTypeConfiguration<RfqQuote>
{
    public void Configure(EntityTypeBuilder<RfqQuote> builder)
    {
        builder.ToTable("RfqQuotes");
        builder.Property(q => q.Notes).HasMaxLength(2000);

        builder.HasIndex(q => new { q.RfqProcessId, q.CompanyId }).IsUnique();

        builder.HasOne(q => q.Company)
            .WithMany()
            .HasForeignKey(q => q.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.LineItems)
            .WithOne(l => l.RfqQuote)
            .HasForeignKey(l => l.RfqQuoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
