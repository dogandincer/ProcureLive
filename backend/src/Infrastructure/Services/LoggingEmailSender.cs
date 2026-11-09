using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>
/// E-posta ayarı kapalı/eksikken kullanılan yedek: ileti gönderilmez, yalnızca loglanır (geliştirme ortamında
/// SMTP olmadan akışların denenebilmesi için). Asıl gönderici <see cref="Email.ConfiguredEmailSender"/>.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[E-POSTA STUB] Alıcı: {ToEmail} | Konu: {Subject} | İçerik: {Body} | Bağlantı: {Link}",
            // Tek satır: log aramaları (ör. test betiklerinin sıfırlama bağlantısını bulması) satır sonunda kopmasın.
            message.To, message.Subject, message.Body.ReplaceLineEndings(" "), message.ActionUrl);

        return Task.CompletedTask;
    }
}
