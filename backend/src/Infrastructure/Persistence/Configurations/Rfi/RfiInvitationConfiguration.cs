using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class RfiInvitationConfiguration : IEntityTypeConfiguration<RfiInvitation>
{
    public void Configure(EntityTypeBuilder<RfiInvitation> builder)
    {
        builder.ToTable("RfiInvitations");
        builder.HasIndex(i => new { i.RfiProcessId, i.CompanyId }).IsUnique();

        builder.HasOne(i => i.Company)
            .WithMany()
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
