using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Workspaces;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Workspaces;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("Workspaces");

        builder.Property(w => w.Name).HasMaxLength(150).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(500);

        builder.HasIndex(w => w.Name).IsUnique();
    }
}
