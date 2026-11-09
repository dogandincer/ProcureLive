using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Infrastructure.Services.Email;

/// <summary>
/// SMTP gönderimi (MailKit). Her ileti için kısa ömürlü bir bağlantı açılır: gönderim hacmi düşük ve dağıtıcı her turda
/// en fazla 50 ileti yolluyor, kalıcı bağlantı yönetiminin (kopma, zaman aşımı) karmaşıklığına değmez.
/// </summary>
public sealed class MailKitEmailTransport : IEmailTransport
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public async Task SendAsync(EmailSettingsSnapshot settings, EmailMessage message, CancellationToken cancellationToken)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(string.IsNullOrWhiteSpace(settings.FromName) ? settings.FromAddress : settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        if (!string.IsNullOrWhiteSpace(settings.ReplyTo))
        {
            mime.ReplyTo.Add(MailboxAddress.Parse(settings.ReplyTo));
        }

        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder
        {
            TextBody = EmailTemplate.RenderText(message),
            HtmlBody = EmailTemplate.RenderHtml(message, settings.FromName)
        }.ToMessageBody();

        using var client = new SmtpClient { Timeout = (int)Timeout.TotalMilliseconds };
        await client.ConnectAsync(settings.Host, settings.Port, MapSecurity(settings.Security), cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            await client.AuthenticateAsync(settings.Username, settings.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private static SecureSocketOptions MapSecurity(EmailSecurityMode mode) => mode switch
    {
        EmailSecurityMode.None => SecureSocketOptions.None,
        EmailSecurityMode.StartTls => SecureSocketOptions.StartTls,
        EmailSecurityMode.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => SecureSocketOptions.Auto
    };
}
