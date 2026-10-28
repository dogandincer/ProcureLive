using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Companies;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Companies.Documents;

/// <summary>
/// Cari belge kartı: vergi levhası, ISO sertifikası, sigorta poliçesi... İç kullanıcı kapsamındaki carinin, tedarikçi yalnızca
/// kendi belgelerini görür ve yönetir. Dosya önce /api/files?purpose=company-document ile yüklenir, burada kaydedilir.
/// </summary>
public static class CompanyDocumentRules
{
    public const int WarningDays = 30;
    public const string UploadPrefix = "/uploads/company-document/";

    public static string ExpiryState(DateTime? validUntil, DateTime now) => validUntil switch
    {
        null => "NoExpiry",
        DateTime d when d.Date < now.Date => "Expired",
        DateTime d when d.Date <= now.Date.AddDays(WarningDays) => "ExpiringSoon",
        _ => "Valid",
    };

    /// <summary>Tedarikçi başka carinin belgelerine erişemez (iç kullanıcı kapsamı ScopeAuthorizationBehavior'da).</summary>
    public static void EnsureOwnCompany(ICurrentUserService user, Guid companyId)
    {
        bool isInternal = user.IsInRole(Roles.Admin) || user.IsInRole(Roles.Buyer);
        if (!isInternal && user.CompanyId != companyId)
            throw new ForbiddenAccessException("Yalnızca kendi firmanızın belgelerini yönetebilirsiniz.");
    }
}

public sealed record GetCompanyDocumentsQuery(Guid CompanyId) : IRequest<IReadOnlyList<CompanyDocumentDto>>, ICompanyScopedRequest;

public sealed class GetCompanyDocumentsQueryHandler : IRequestHandler<GetCompanyDocumentsQuery, IReadOnlyList<CompanyDocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public GetCompanyDocumentsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<CompanyDocumentDto>> Handle(GetCompanyDocumentsQuery r, CancellationToken cancellationToken)
    {
        CompanyDocumentRules.EnsureOwnCompany(_currentUser, r.CompanyId);
        DateTime now = _dateTime.UtcNow;
        var rows = await _context.CompanyDocuments.AsNoTracking()
            .Where(d => d.CompanyId == r.CompanyId)
            .OrderBy(d => d.ValidUntil == null).ThenBy(d => d.ValidUntil).ThenBy(d => d.Title)
            .ToListAsync(cancellationToken);
        return rows.Select(d => new CompanyDocumentDto(
            d.Id, d.DocumentType.ToString(), d.Title, d.FileUrl, d.FileName, d.ValidUntil,
            CompanyDocumentRules.ExpiryState(d.ValidUntil, now), d.CreatedAt)).ToList();
    }
}

[RequiresPermission(PermissionCodes.CompaniesManage)]
public sealed record AddCompanyDocumentCommand(
    Guid CompanyId, CompanyDocumentType DocumentType, string Title, string FileUrl, string FileName, DateTime? ValidUntil)
    : IRequest<Guid>, ICompanyScopedRequest;

[RequiresPermission(PermissionCodes.CompaniesManage)]
public sealed record DeleteCompanyDocumentCommand(Guid CompanyId, Guid DocumentId) : IRequest, ICompanyScopedRequest;

public sealed class AddCompanyDocumentCommandValidator : AbstractValidator<AddCompanyDocumentCommand>
{
    public AddCompanyDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentType).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().WithMessage("Belge adı zorunludur.").MaximumLength(200);
        RuleFor(x => x.FileName).MaximumLength(255);
        RuleFor(x => x.FileUrl).NotEmpty().WithMessage("Belge dosyası yüklenmelidir.")
            .Must(u => u.StartsWith(CompanyDocumentRules.UploadPrefix, StringComparison.Ordinal))
            .WithMessage("Belge dosyası sistem üzerinden yüklenmelidir.");
    }
}

public sealed class CompanyDocumentCommandHandlers :
    IRequestHandler<AddCompanyDocumentCommand, Guid>,
    IRequestHandler<DeleteCompanyDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorage _storage;

    public CompanyDocumentCommandHandlers(IApplicationDbContext context, ICurrentUserService currentUser, IFileStorage storage)
    {
        _context = context;
        _currentUser = currentUser;
        _storage = storage;
    }

    public async Task<Guid> Handle(AddCompanyDocumentCommand r, CancellationToken cancellationToken)
    {
        CompanyDocumentRules.EnsureOwnCompany(_currentUser, r.CompanyId);
        if (!await _context.Companies.AnyAsync(c => c.Id == r.CompanyId, cancellationToken))
            throw new NotFoundException(nameof(Company), r.CompanyId);

        var document = new CompanyDocument
        {
            CompanyId = r.CompanyId,
            DocumentType = r.DocumentType,
            Title = r.Title.Trim(),
            FileUrl = r.FileUrl,
            FileName = string.IsNullOrWhiteSpace(r.FileName) ? Path.GetFileName(r.FileUrl) : r.FileName.Trim(),
            // Tarih-only alan: istemci saat dilimi/Kind ne gönderirse göndersin gün olarak UTC saklanır.
            ValidUntil = r.ValidUntil is DateTime v ? DateTime.SpecifyKind(v.Date, DateTimeKind.Utc) : null,
            UploadedByUserId = _currentUser.UserId,
        };
        _context.CompanyDocuments.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document.Id;
    }

    public async Task Handle(DeleteCompanyDocumentCommand r, CancellationToken cancellationToken)
    {
        CompanyDocumentRules.EnsureOwnCompany(_currentUser, r.CompanyId);
        CompanyDocument document = await _context.CompanyDocuments
            .FirstOrDefaultAsync(d => d.Id == r.DocumentId && d.CompanyId == r.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(CompanyDocument), r.DocumentId);
        _context.CompanyDocuments.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(document.FileUrl, cancellationToken);
    }
}

/// <summary>
/// Zamanlanmış görev (WebApi hosted service çağırır): geçerliliği 30 gün içinde dolacak ya da dolmuş belgeler için bir kez
/// tedarikçiye ve carinin işyerindeki iç kullanıcılara uyarı gönderir (<see cref="CompanyDocument.ExpiryWarnedAt"/>).
/// </summary>
public sealed record WarnExpiringCompanyDocumentsCommand : IRequest<int>;

public sealed class WarnExpiringCompanyDocumentsCommandHandler : IRequestHandler<WarnExpiringCompanyDocumentsCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;
    private readonly InternalNotificationAudience _audience;

    public WarnExpiringCompanyDocumentsCommandHandler(
        IApplicationDbContext context, IDateTime dateTime, INotificationRealtimeService realtime, InternalNotificationAudience audience)
    {
        _context = context;
        _dateTime = dateTime;
        _realtime = realtime;
        _audience = audience;
    }

    public async Task<int> Handle(WarnExpiringCompanyDocumentsCommand request, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;
        DateTime limit = now.Date.AddDays(CompanyDocumentRules.WarningDays);
        List<CompanyDocument> due = await _context.CompanyDocuments.Include(d => d.Company).ThenInclude(c => c.Workspace)
            .Where(d => d.ValidUntil != null && d.ValidUntil <= limit && d.ExpiryWarnedAt == null)
            .Take(200)
            .ToListAsync(cancellationToken);

        foreach (CompanyDocument d in due)
        {
            d.ExpiryWarnedAt = now;
            bool expired = d.ValidUntil!.Value.Date < now.Date;
            string when = d.ValidUntil.Value.ToString("dd.MM.yyyy");
            string title = expired ? "Belge Süresi Doldu" : "Belge Süresi Doluyor";
            string body = expired
                ? $"\"{d.Title}\" belgesinin geçerliliği {when} tarihinde doldu. Güncel belgeyi yükleyin."
                : $"\"{d.Title}\" belgesinin geçerliliği {when} tarihinde dolacak. Güncel belgeyi yükleyin.";

            await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, d.CompanyId, NotificationType.DocumentExpiry,
                title, body, nameof(Company), d.CompanyId, cancellationToken);

            IReadOnlyList<Guid> audience = d.Company.Workspace?.FirmId is Guid firmId
                ? await _audience.ForScopeAsync(d.Company.WorkspaceId, firmId, null, null, cancellationToken)
                : await _audience.AdminsAsync(cancellationToken);
            await NotificationCreator.CreateForInternalUsersAsync(_context, _realtime, _dateTime, audience, NotificationType.DocumentExpiry,
                title, $"{d.Company.Name}: {body}", nameof(Company), d.CompanyId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return due.Count;
    }
}
