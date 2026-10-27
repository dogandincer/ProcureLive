using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyWorkspaceConfiguration : IEntityTypeConfiguration<CompanyWorkspace>
{
    public void Configure(EntityTypeBuilder<CompanyWorkspace> builder)
    {
        builder.ToTable("CompanyWorkspaces");

        builder.HasIndex(x => new { x.CompanyId, x.WorkspaceId }).IsUnique();

        builder.HasOne(x => x.Company)
            .WithMany(c => c.VisibleWorkspaces)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
