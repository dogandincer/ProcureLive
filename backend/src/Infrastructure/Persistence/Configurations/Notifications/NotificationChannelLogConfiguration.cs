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
        builder.Property(c => c.Recipient).HasMaxLength(320);

        // Dağıtıcının her 10 sn'deki sorgusu: kanal + durum (bekleyen e-postalar).
        builder.HasIndex(c => new { c.Channel, c.Status });
    }
}
