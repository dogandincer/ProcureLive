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

        // İşyeri adı firma içinde benzersizdir (iki firmanın da "Merkez" işyeri olabilir).
        builder.HasIndex(w => new { w.FirmId, w.Name }).IsUnique();

        builder.HasOne(w => w.Firm)
            .WithMany()
            .HasForeignKey(w => w.FirmId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
