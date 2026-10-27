using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class RfiProcessConfiguration : IEntityTypeConfiguration<RfiProcess>
{
    public void Configure(EntityTypeBuilder<RfiProcess> builder)
    {
        builder.ToTable("RfiProcesses");
        builder.Property(p => p.Title).HasMaxLength(250).IsRequired();

        builder.HasOne(p => p.RfiTemplate)
            .WithMany()
            .HasForeignKey(p => p.RfiTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.WorkspaceId);

        builder.HasOne<ProcureLive.Domain.Entities.Workspaces.Workspace>()
            .WithMany()
            .HasForeignKey(p => p.WorkspaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.OwnerCompany)
            .WithMany()
            .HasForeignKey(p => p.OwnerCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Invitations)
            .WithOne(i => i.RfiProcess)
            .HasForeignKey(i => i.RfiProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Responses)
            .WithOne(r => r.RfiProcess)
            .HasForeignKey(r => r.RfiProcessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Evaluations)
            .WithOne(e => e.RfiProcess)
            .HasForeignKey(e => e.RfiProcessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
