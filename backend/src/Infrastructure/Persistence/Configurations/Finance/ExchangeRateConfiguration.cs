using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Finance;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");

        builder.Property(r => r.RateToBase).HasPrecision(18, 6).IsRequired();
        builder.Property(r => r.Source).HasMaxLength(100);

        builder.HasIndex(r => new { r.CurrencyId, r.RateDate }).IsUnique();

        builder.HasOne(r => r.Currency)
            .WithMany(c => c.ExchangeRates)
            .HasForeignKey(r => r.CurrencyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
