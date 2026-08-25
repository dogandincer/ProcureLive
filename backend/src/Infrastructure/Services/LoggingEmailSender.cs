using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>
/// Phase 3 stub: no SMTP provider is configured yet, so outbound email is logged instead of sent.
/// Swap this registration for a real provider (SendGrid, SES, SMTP) in DependencyInjection.cs later —
/// nothing outside Infrastructure needs to change since callers only depend on IEmailSender.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[E-POSTA STUB] Alıcı: {ToEmail} | Konu: {Subject} | İçerik: {Body}", toEmail, subject, body);

        return Task.CompletedTask;
    }
}
