using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Erp;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Erp;

public sealed class ErpSettingsConfiguration : IEntityTypeConfiguration<ErpSettings>
{
    public void Configure(EntityTypeBuilder<ErpSettings> builder)
    {
        builder.ToTable("ErpSettings");
        builder.Property(s => s.Mode).HasConversion<int>();
        builder.Property(s => s.GetServiceUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.SaveServiceUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Username).HasMaxLength(150).IsRequired();
        builder.Property(s => s.PasswordProtected).HasMaxLength(2000);
        builder.Property(s => s.CoCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.BranchCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.OfferDocTraCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.OrderDocTraCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.DocNoPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.EntityCodePrefix).HasMaxLength(30);
    }
}

public sealed class ErpSyncJobConfiguration : IEntityTypeConfiguration<ErpSyncJob>
{
    public void Configure(EntityTypeBuilder<ErpSyncJob> builder)
    {
        builder.ToTable("ErpSyncJobs");
        builder.Property(j => j.Summary).HasMaxLength(300).IsRequired();
        builder.Property(j => j.Message).HasMaxLength(1000);
        builder.HasIndex(j => new { j.Status, j.NextAttemptAt });
        builder.HasIndex(j => j.AuctionId);
        builder.HasIndex(j => j.CompanyId);
        builder.HasIndex(j => j.PurchaseOrderId);
    }
}

public sealed class ErpOfferLinkConfiguration : IEntityTypeConfiguration<ErpOfferLink>
{
    public void Configure(EntityTypeBuilder<ErpOfferLink> builder)
    {
        builder.ToTable("ErpOfferLinks");
        builder.Property(l => l.DocNo).HasMaxLength(40).IsRequired();
        builder.HasIndex(l => new { l.AuctionId, l.CompanyId }).IsUnique();
    }
}
