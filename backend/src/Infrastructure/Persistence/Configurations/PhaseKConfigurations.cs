using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Collaboration;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Orders;

namespace ProcureLive.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.Property(o => o.OrderNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ErpOrderDocNo).HasMaxLength(40);
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.Property(o => o.SourceTitle).HasMaxLength(300);
        builder.Property(o => o.Notes).HasMaxLength(2000);
        builder.Property(o => o.SupplierNote).HasMaxLength(1000);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 4);
        builder.HasIndex(o => new { o.SourceKind, o.SourceId, o.SupplierCompanyId });
        builder.HasIndex(o => o.WorkspaceId);
        builder.HasOne(o => o.OwnerCompany).WithMany().HasForeignKey(o => o.OwnerCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.SupplierCompany).WithMany().HasForeignKey(o => o.SupplierCompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Currency).WithMany().HasForeignKey(o => o.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Lines).WithOne(l => l.PurchaseOrder).HasForeignKey(l => l.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");
        builder.Property(l => l.Description).HasMaxLength(500);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineTotal).HasPrecision(18, 4);
    }
}

public sealed class ProcessQuestionConfiguration : IEntityTypeConfiguration<ProcessQuestion>
{
    public void Configure(EntityTypeBuilder<ProcessQuestion> builder)
    {
        builder.ToTable("ProcessQuestions");
        builder.Property(q => q.Question).HasMaxLength(2000).IsRequired();
        builder.Property(q => q.Answer).HasMaxLength(4000);
        builder.HasIndex(q => new { q.ProcessKind, q.ProcessId });
        builder.HasOne(q => q.Company).WithMany().HasForeignKey(q => q.CompanyId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyDocument> builder)
    {
        builder.ToTable("CompanyDocuments");
        builder.Property(d => d.Title).HasMaxLength(200).IsRequired();
        builder.Property(d => d.FileUrl).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255);
        builder.HasIndex(d => d.CompanyId);
        builder.HasIndex(d => d.ValidUntil);
        builder.HasOne(d => d.Company).WithMany().HasForeignKey(d => d.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
