using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfq;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfq;

public sealed class RfqInvitationConfiguration : IEntityTypeConfiguration<RfqInvitation>
{
    public void Configure(EntityTypeBuilder<RfqInvitation> builder)
    {
        builder.ToTable("RfqInvitations");
        builder.HasIndex(i => new { i.RfqProcessId, i.CompanyId }).IsUnique();

        builder.HasOne(i => i.Company)
            .WithMany()
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
