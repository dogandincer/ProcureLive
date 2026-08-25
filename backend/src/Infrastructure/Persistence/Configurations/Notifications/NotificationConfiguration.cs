using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Notifications;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.Property(n => n.Title).HasMaxLength(250).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);

        builder.HasIndex(n => n.UserId);
        builder.HasIndex(n => n.CompanyId);

        builder.HasMany(n => n.ChannelLogs)
            .WithOne(c => c.Notification)
            .HasForeignKey(c => c.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
