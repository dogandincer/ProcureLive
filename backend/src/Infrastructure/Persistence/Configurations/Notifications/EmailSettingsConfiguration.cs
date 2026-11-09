using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Notifications;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Notifications;

public sealed class EmailSettingsConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        builder.ToTable("EmailSettings");
        builder.Property(s => s.Host).HasMaxLength(255).IsRequired();
        builder.Property(s => s.Security).HasConversion<int>();
        builder.Property(s => s.Username).HasMaxLength(255);
        builder.Property(s => s.SmtpPasswordProtected).HasMaxLength(2000);
        builder.Property(s => s.FromAddress).HasMaxLength(320).IsRequired();
        builder.Property(s => s.FromName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.ReplyTo).HasMaxLength(320);
        builder.Property(s => s.DisabledNotificationTypes).IsRequired();
    }
}
