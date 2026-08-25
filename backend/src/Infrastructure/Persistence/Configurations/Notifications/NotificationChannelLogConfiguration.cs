using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Notifications;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationChannelLogConfiguration : IEntityTypeConfiguration<NotificationChannelLog>
{
    public void Configure(EntityTypeBuilder<NotificationChannelLog> builder)
    {
        builder.ToTable("NotificationChannelLogs");
        builder.Property(c => c.ErrorMessage).HasMaxLength(1000);
    }
}
