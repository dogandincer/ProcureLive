using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Finance;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("Currencies");

        builder.Property(c => c.Code).HasMaxLength(3).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Symbol).HasMaxLength(10).IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();
    }
}
