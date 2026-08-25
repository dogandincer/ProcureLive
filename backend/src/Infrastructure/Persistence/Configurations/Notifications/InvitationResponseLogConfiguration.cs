using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Notifications;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Notifications;

public sealed class InvitationResponseLogConfiguration : IEntityTypeConfiguration<InvitationResponseLog>
{
    public void Configure(EntityTypeBuilder<InvitationResponseLog> builder)
    {
        builder.ToTable("InvitationResponseLogs");
        builder.Property(l => l.Reason).HasMaxLength(1000);

        builder.HasIndex(l => new { l.SourceType, l.SourceId, l.CompanyId });

        builder.HasOne(l => l.Company)
            .WithMany()
            .HasForeignKey(l => l.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
