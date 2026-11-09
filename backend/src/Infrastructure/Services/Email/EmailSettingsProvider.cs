using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Infrastructure.Services.Email;

/// <summary>
/// Kayıtlı e-posta ayarını şifresi çözülmüş halde 30 sn önbellekler (singleton). Kayıt ekranı <see cref="Invalidate"/> çağırır;
/// süre sınırı, ayarın başka bir API örneğinden değiştirildiği durumda da en geç 30 sn'de güncellenmesini sağlar.
/// </summary>
public sealed class EmailSettingsProvider : IEmailSettingsProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISecretProtector _protector;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private (EmailSettingsSnapshot Value, DateTime LoadedAt)? _cached;

    public EmailSettingsProvider(IServiceScopeFactory scopeFactory, ISecretProtector protector)
    {
        _scopeFactory = scopeFactory;
        _protector = protector;
    }

    public async Task<EmailSettingsSnapshot> GetAsync(CancellationToken cancellationToken)
    {
        if (_cached is { } hit && DateTime.UtcNow - hit.LoadedAt < CacheDuration) return hit.Value;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cached is { } again && DateTime.UtcNow - again.LoadedAt < CacheDuration) return again.Value;

            using IServiceScope scope = _scopeFactory.CreateScope();
            IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            EmailSettings? row = await context.EmailSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

            EmailSettingsSnapshot snapshot = row is null
                ? EmailSettingsSnapshot.Disabled
                : new EmailSettingsSnapshot(
                    row.Enabled, row.Host, row.Port, row.Security, row.Username,
                    row.SmtpPasswordProtected is null ? null : _protector.Unprotect(row.SmtpPasswordProtected),
                    row.FromAddress, row.FromName, row.ReplyTo,
                    row.DisabledNotificationTypes.Select(t => (NotificationType)t).ToHashSet());

            _cached = (snapshot, DateTime.UtcNow);
            return snapshot;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate() => _cached = null;
}
