using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Workspaces;
using ProcureLive.Infrastructure.Identity;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Workspaces;

public sealed class UserWorkspaceAccessConfiguration : IEntityTypeConfiguration<UserWorkspaceAccess>
{
    public void Configure(EntityTypeBuilder<UserWorkspaceAccess> builder)
    {
        builder.ToTable("UserWorkspaceAccesses");

        builder.HasIndex(a => new { a.UserId, a.WorkspaceId }).IsUnique();

        builder.HasOne(a => a.Workspace)
            .WithMany()
            .HasForeignKey(a => a.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
