using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services.Email;

/// <summary>
/// Uygulamanın <see cref="IEmailSender"/>'ı: Admin'in e-posta ayarı açık ve tamsa SMTP ile gönderir (hata istisna olarak döner,
/// dağıtıcı yeniden dener); değilse eski davranışa düşer ve iletiyi yalnızca loglar.
/// </summary>
public sealed class ConfiguredEmailSender : IEmailSender
{
    private readonly IEmailSettingsProvider _settings;
    private readonly IEmailTransport _transport;
    private readonly LoggingEmailSender _fallback;

    public ConfiguredEmailSender(IEmailSettingsProvider settings, IEmailTransport transport, LoggingEmailSender fallback)
    {
        _settings = settings;
        _transport = transport;
        _fallback = fallback;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        EmailSettingsSnapshot settings = await _settings.GetAsync(cancellationToken);
        if (!settings.IsActive)
        {
            await _fallback.SendAsync(message, cancellationToken);
            return;
        }

        await _transport.SendAsync(settings, message, cancellationToken);
    }
}
