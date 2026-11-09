using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Notifications;
using ProcureLive.Domain.Entities.Notifications;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Notifications.Email;

// Erişim denetimi controller'da (Policies.RequireAdmin): bu ekran yalnızca Admin'e açıktır.

// ---------------------------------------------------------------- Ayarı oku

public sealed record GetEmailSettingsQuery : IRequest<EmailSettingsDto>;

public sealed class GetEmailSettingsQueryHandler : IRequestHandler<GetEmailSettingsQuery, EmailSettingsDto>
{
    private readonly IApplicationDbContext _context;

    public GetEmailSettingsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmailSettingsDto> Handle(GetEmailSettingsQuery request, CancellationToken cancellationToken)
    {
        EmailSettings settings = await _context.EmailSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new EmailSettings();

        return new EmailSettingsDto(
            settings.Enabled,
            settings.Host,
            settings.Port,
            settings.Security.ToString(),
            settings.Username,
            !string.IsNullOrEmpty(settings.SmtpPasswordProtected),
            settings.FromAddress,
            settings.FromName,
            settings.ReplyTo,
            settings.DisabledNotificationTypes.Select(t => ((NotificationType)t).ToString()).ToArray(),
            Enum.GetNames<NotificationType>(),
            settings.UpdatedAt ?? (settings.CreatedAt == default ? null : settings.CreatedAt),
            settings.UpdatedBy ?? settings.CreatedBy);
    }
}

// ---------------------------------------------------------------- Ayarı kaydet

public sealed record UpdateEmailSettingsCommand(EmailSettingsRequest Settings) : IRequest;

public sealed class EmailSettingsRequestValidator : AbstractValidator<EmailSettingsRequest>
{
    public EmailSettingsRequestValidator()
    {
        RuleFor(x => x.Host).NotEmpty().When(x => x.Enabled).WithMessage("SMTP sunucusu boş olamaz.");
        RuleFor(x => x.Host).MaximumLength(255);
        RuleFor(x => x.Port).InclusiveBetween(1, 65535).WithMessage("Port 1 ile 65535 arasında olmalıdır.");
        RuleFor(x => x.Security).Must(s => Enum.TryParse<EmailSecurityMode>(s, out _)).WithMessage("Geçersiz bağlantı güvenliği.");
        RuleFor(x => x.FromAddress).NotEmpty().When(x => x.Enabled).WithMessage("Gönderen e-posta adresi boş olamaz.");
        RuleFor(x => x.FromAddress).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.FromAddress)).WithMessage("Gönderen e-posta adresi geçerli değil.");
        RuleFor(x => x.FromName).MaximumLength(150);
        RuleFor(x => x.ReplyTo).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ReplyTo)).WithMessage("Yanıt adresi geçerli değil.");
        RuleFor(x => x.Username).MaximumLength(255);
        RuleFor(x => x.Password).MaximumLength(500);
        RuleForEach(x => x.DisabledNotificationTypes).Must(t => Enum.TryParse<NotificationType>(t, out _)).WithMessage("Geçersiz bildirim türü.");
    }
}

public sealed class UpdateEmailSettingsCommandValidator : AbstractValidator<UpdateEmailSettingsCommand>
{
    public UpdateEmailSettingsCommandValidator()
    {
        RuleFor(x => x.Settings).NotNull().SetValidator(new EmailSettingsRequestValidator());
    }
}

public sealed class UpdateEmailSettingsCommandHandler : IRequestHandler<UpdateEmailSettingsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ISecretProtector _protector;
    private readonly IEmailSettingsProvider _provider;

    public UpdateEmailSettingsCommandHandler(IApplicationDbContext context, ISecretProtector protector, IEmailSettingsProvider provider)
    {
        _context = context;
        _protector = protector;
        _provider = provider;
    }

    public async Task Handle(UpdateEmailSettingsCommand request, CancellationToken cancellationToken)
    {
        EmailSettingsRequest r = request.Settings;
        EmailSettings? settings = await _context.EmailSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new EmailSettings();
            _context.EmailSettings.Add(settings);
        }

        settings.Enabled = r.Enabled;
        settings.Host = r.Host.Trim();
        settings.Port = r.Port;
        settings.Security = Enum.Parse<EmailSecurityMode>(r.Security);
        settings.Username = string.IsNullOrWhiteSpace(r.Username) ? null : r.Username.Trim();
        settings.FromAddress = r.FromAddress.Trim();
        settings.FromName = r.FromName.Trim();
        settings.ReplyTo = string.IsNullOrWhiteSpace(r.ReplyTo) ? null : r.ReplyTo.Trim();
        settings.DisabledNotificationTypes = EmailSettingsMapping.ParseTypes(r.DisabledNotificationTypes).Select(t => (int)t).OrderBy(t => t).ToArray();

        if (r.ClearPassword)
            settings.SmtpPasswordProtected = null;
        else if (!string.IsNullOrEmpty(r.Password))
            settings.SmtpPasswordProtected = _protector.Protect(r.Password);

        await _context.SaveChangesAsync(cancellationToken);
        _provider.Invalidate();
    }
}

// ---------------------------------------------------------------- Test e-postası

/// <summary>
/// Formdaki (henüz kaydedilmemiş olabilir) ayarla tek bir test iletisi gönderir; şifre boşsa kayıtlı şifre kullanılır.
/// "Açık" anahtarına bakmaz: Admin gönderimi açmadan önce ayarı deneyebilmelidir. SMTP hatası istisna değil sonuç olarak döner.
/// </summary>
public sealed record SendTestEmailCommand(string To, EmailSettingsRequest Settings) : IRequest<TestEmailResultDto>;

public sealed class SendTestEmailCommandValidator : AbstractValidator<SendTestEmailCommand>
{
    public SendTestEmailCommandValidator()
    {
        RuleFor(x => x.To).NotEmpty().EmailAddress().WithMessage("Test için geçerli bir alıcı e-posta adresi girin.");
        RuleFor(x => x.Settings).NotNull().SetValidator(new EmailSettingsRequestValidator());
        RuleFor(x => x.Settings.Host).NotEmpty().WithMessage("SMTP sunucusu boş olamaz.");
        RuleFor(x => x.Settings.FromAddress).NotEmpty().WithMessage("Gönderen e-posta adresi boş olamaz.");
    }
}

public sealed class SendTestEmailCommandHandler : IRequestHandler<SendTestEmailCommand, TestEmailResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISecretProtector _protector;
    private readonly IEmailTransport _transport;
    private readonly IAppUrlProvider _urls;
    private readonly IDateTime _dateTime;

    public SendTestEmailCommandHandler(
        IApplicationDbContext context, ISecretProtector protector, IEmailTransport transport, IAppUrlProvider urls, IDateTime dateTime)
    {
        _context = context;
        _protector = protector;
        _transport = transport;
        _urls = urls;
        _dateTime = dateTime;
    }

    public async Task<TestEmailResultDto> Handle(SendTestEmailCommand request, CancellationToken cancellationToken)
    {
        EmailSettingsRequest r = request.Settings;

        string? password = r.ClearPassword ? null : r.Password;
        if (string.IsNullOrEmpty(password) && !r.ClearPassword)
        {
            string? stored = await _context.EmailSettings.AsNoTracking().Select(s => s.SmtpPasswordProtected).FirstOrDefaultAsync(cancellationToken);
            password = stored is null ? null : _protector.Unprotect(stored);
        }

        var snapshot = new EmailSettingsSnapshot(
            true, r.Host.Trim(), r.Port, Enum.Parse<EmailSecurityMode>(r.Security),
            string.IsNullOrWhiteSpace(r.Username) ? null : r.Username.Trim(), password,
            r.FromAddress.Trim(), r.FromName.Trim(), string.IsNullOrWhiteSpace(r.ReplyTo) ? null : r.ReplyTo.Trim(),
            new HashSet<NotificationType>());

        var message = new EmailMessage(
            request.To.Trim(),
            "Test e-postası",
            $"Bu ileti e-posta ayarlarını denemek için gönderildi ({_dateTime.UtcNow:dd.MM.yyyy HH:mm} UTC).\n\nBu iletiyi görüyorsanız SMTP ayarı çalışıyor.",
            $"{_urls.FrontendBaseUrl}/admin/email-settings",
            "E-posta ayarlarını aç");

        try
        {
            await _transport.SendAsync(snapshot, message, cancellationToken);
            return new TestEmailResultDto(true, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return new TestEmailResultDto(false, EmailErrors.Describe(ex));
        }
    }
}

// ---------------------------------------------------------------- Gönderim kayıtları

public sealed record GetEmailLogsQuery(string? Status, string? Search, int Page, int PageSize) : IRequest<PagedResult<EmailLogDto>>;

public sealed record GetEmailLogSummaryQuery : IRequest<EmailLogSummaryDto>;

public sealed class GetEmailLogsQueryHandler :
    IRequestHandler<GetEmailLogsQuery, PagedResult<EmailLogDto>>,
    IRequestHandler<GetEmailLogSummaryQuery, EmailLogSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetEmailLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EmailLogDto>> Handle(GetEmailLogsQuery request, CancellationToken cancellationToken)
    {
        int page = Math.Max(1, request.Page);
        int pageSize = Math.Clamp(request.PageSize, 1, 100);

        IQueryable<NotificationChannelLog> query = _context.NotificationChannelLogs.AsNoTracking()
            .Where(l => l.Channel == NotificationChannel.Email);

        if (Enum.TryParse(request.Status, out NotificationDeliveryStatus status))
            query = query.Where(l => l.Status == status);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(l => (l.Recipient != null && l.Recipient.ToLower().Contains(term)) || l.Notification.Title.ToLower().Contains(term));
        }

        int total = await query.CountAsync(cancellationToken);
        List<EmailLogDto> items = await query
            .OrderByDescending(l => l.Notification.CreatedAt).ThenBy(l => l.Recipient)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new EmailLogDto(
                l.Id, l.Recipient, l.Notification.Title, l.Notification.Type.ToString(), l.Status.ToString(),
                l.AttemptCount, l.ErrorMessage, l.Notification.CreatedAt, l.SentAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<EmailLogDto>(items, total, page, pageSize);
    }

    public async Task<EmailLogSummaryDto> Handle(GetEmailLogSummaryQuery request, CancellationToken cancellationToken)
    {
        Dictionary<NotificationDeliveryStatus, int> counts = await _context.NotificationChannelLogs.AsNoTracking()
            .Where(l => l.Channel == NotificationChannel.Email)
            .GroupBy(l => l.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        int Get(NotificationDeliveryStatus s) => counts.TryGetValue(s, out int n) ? n : 0;
        return new EmailLogSummaryDto(
            Get(NotificationDeliveryStatus.Pending), Get(NotificationDeliveryStatus.Sent),
            Get(NotificationDeliveryStatus.Failed), Get(NotificationDeliveryStatus.Skipped));
    }
}

/// <summary>Başarısız bir e-postayı yeniden kuyruğa alır (deneme sayacı sıfırlanır).</summary>
public sealed record RetryEmailCommand(Guid LogId) : IRequest;

public sealed class RetryEmailCommandHandler : IRequestHandler<RetryEmailCommand>
{
    private readonly IApplicationDbContext _context;

    public RetryEmailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RetryEmailCommand request, CancellationToken cancellationToken)
    {
        NotificationChannelLog log = await _context.NotificationChannelLogs
            .FirstOrDefaultAsync(l => l.Id == request.LogId && l.Channel == NotificationChannel.Email, cancellationToken)
            ?? throw new NotFoundException(nameof(NotificationChannelLog), request.LogId);

        if (log.Status != NotificationDeliveryStatus.Failed || log.Recipient is null)
            throw new ConflictException("Yalnızca alıcısı belli olan başarısız e-postalar yeniden denenebilir.");

        log.Status = NotificationDeliveryStatus.Pending;
        log.AttemptCount = 0;
        log.NextAttemptAt = null;
        log.ErrorMessage = null;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

// ---------------------------------------------------------------- Ortak

internal static class EmailSettingsMapping
{
    public static IEnumerable<NotificationType> ParseTypes(IEnumerable<string>? names) =>
        (names ?? []).Select(n => Enum.TryParse(n, out NotificationType t) ? (NotificationType?)t : null).OfType<NotificationType>().Distinct();
}

public static class EmailErrors
{
    /// <summary>SMTP istisnasını Admin'in anlayacağı kısa bir metne çevirir (iç içe istisnalardaki asıl nedeni de ekler).</summary>
    public static string Describe(Exception ex)
    {
        string message = ex.Message;
        if (ex.InnerException is { } inner && !message.Contains(inner.Message, StringComparison.Ordinal))
            message = $"{message} ({inner.Message})";
        return message.Length > 900 ? message[..900] : message;
    }
}
