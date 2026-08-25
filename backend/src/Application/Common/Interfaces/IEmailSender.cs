namespace ProcureLive.Application.Common.Interfaces;

/// <summary>
/// Outbound email abstraction. Phase 3 ships a logging-only implementation (no SMTP configured yet);
/// swapping in a real provider later only touches Infrastructure, not Application.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken);
}
