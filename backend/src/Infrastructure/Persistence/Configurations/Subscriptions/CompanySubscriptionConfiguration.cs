using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Subscriptions;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Subscriptions;

public sealed class CompanySubscriptionConfiguration : IEntityTypeConfiguration<CompanySubscription>
{
    public void Configure(EntityTypeBuilder<CompanySubscription> builder)
    {
        builder.ToTable("CompanySubscriptions");

        builder.Property(s => s.PriceUsd).HasPrecision(18, 2);
        builder.Property(s => s.PaymentReference).HasMaxLength(100).IsRequired();

        // Plus kontrolü (CompanyId + Status + ExpiresAt) her istekte çalışır.
        builder.HasIndex(s => new { s.CompanyId, s.Status, s.ExpiresAt });

        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
