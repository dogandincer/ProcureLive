using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Notifications;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Collaboration;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Collaboration;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Entities.Rfq;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Collaboration;

/// <summary>Sürecin erişim ve bildirim için gereken özeti (ihale / RFI / RFQ ortak).</summary>
internal sealed record ProcessFacts(
    ProcessKind Kind, Guid Id, string Title, Guid OwnerCompanyId, Guid? WorkspaceId, Guid? ResponsibleUserId, string? CreatedBy,
    bool IsOpen, IReadOnlyList<Guid> ParticipantCompanyIds)
{
    public string RelatedType => Kind switch
    {
        ProcessKind.Auction => nameof(Auction),
        ProcessKind.Rfi => nameof(RfiProcess),
        _ => nameof(RfqProcess),
    };

    public string KindLabel => Kind switch { ProcessKind.Auction => "ihalesi", ProcessKind.Rfi => "RFI süreci", _ => "RFQ süreci" };
}

/// <summary>
/// Soru-cevap erişimi: iç kullanıcı sürecin kapsamında olmalı; tedarikçi sürece (reddetmemiş) davetli olmalı.
/// Tedarikçi başka firmanın sorusunu yalnızca herkese açık cevaplandıysa ve firma adı olmadan görür.
/// </summary>
internal sealed class ProcessQuestionAccess
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _scopes;
    private readonly ISupplierAccessService _supplierAccess;

    public ProcessQuestionAccess(
        IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes, ISupplierAccessService supplierAccess)
    {
        _context = context;
        _currentUser = currentUser;
        _scopes = scopes;
        _supplierAccess = supplierAccess;
    }

    public bool IsInternal => _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);

    public async Task<ProcessFacts> LoadAsync(ProcessKind kind, Guid id, CancellationToken ct)
    {
        ProcessFacts facts = kind switch
        {
            ProcessKind.Auction => await _context.Auctions.Where(a => a.Id == id).Select(a => new ProcessFacts(
                    ProcessKind.Auction, a.Id, a.Title, a.OwnerCompanyId, a.WorkspaceId, a.ResponsibleUserId, a.CreatedBy,
                    a.Status == AuctionStatus.Scheduled || a.Status == AuctionStatus.Live || a.Status == AuctionStatus.Paused,
                    a.Invitations.Where(i => i.Status != InvitationStatus.Declined).Select(i => i.CompanyId).ToList()))
                .FirstOrDefaultAsync(ct),
            ProcessKind.Rfi => await _context.RfiProcesses.Where(p => p.Id == id).Select(p => new ProcessFacts(
                    ProcessKind.Rfi, p.Id, p.Title, p.OwnerCompanyId, p.WorkspaceId, null, p.CreatedBy,
                    p.Status == ProcessStatus.Sent || p.Status == ProcessStatus.InProgress,
                    p.Invitations.Where(i => i.Status != InvitationStatus.Declined).Select(i => i.CompanyId).ToList()))
                .FirstOrDefaultAsync(ct),
            _ => await _context.RfqProcesses.Where(p => p.Id == id).Select(p => new ProcessFacts(
                    ProcessKind.Rfq, p.Id, p.Title, p.OwnerCompanyId, p.WorkspaceId, null, p.CreatedBy,
                    p.Status == ProcessStatus.Sent || p.Status == ProcessStatus.InProgress,
                    p.Invitations.Where(i => i.Status != InvitationStatus.Declined).Select(i => i.CompanyId).ToList()))
                .FirstOrDefaultAsync(ct),
        } ?? throw new NotFoundException(kind.ToString(), id);

        if (IsInternal)
        {
            if (_currentUser.IsInRole(Roles.Admin)) return facts;
            DataScope scope = await _scopes.GetAsync(ct);
            if (!scope.HasOwned(facts.WorkspaceId, facts.OwnerCompanyId))
                throw new ForbiddenAccessException("Bu kayıt yetkili olduğunuz firma/işyerlerine ait değil.");
            return facts;
        }

        if (_currentUser.CompanyId is not Guid companyId || !facts.ParticipantCompanyIds.Contains(companyId))
            throw new NotFoundException(kind.ToString(), id);

        // Süreç ekranı kısıtlı görünümdeyse (ör. Standart kotası dolu, davet henüz kabul edilmemiş) soru-cevap da kapalıdır.
        SupplierAccessContext access = await _supplierAccess.GetContextAsync(companyId, ct);
        AuctionAccessLevel level = kind switch
        {
            ProcessKind.Auction => access.Evaluate(await _context.Auctions.Where(a => a.Id == id)
                .Select(a => new AuctionAccessFacts(a.Id, a.Status, a.OwnerCompanyId, a.IsListedForPlus, a.OwnerCompany.ListTendersForPlus))
                .FirstAsync(ct)),
            ProcessKind.Rfi => ProcessParticipationRules.Evaluate(access, await _context.RfiInvitations
                .Where(i => i.RfiProcessId == id && i.CompanyId == companyId).Select(i => (InvitationStatus?)i.Status).FirstOrDefaultAsync(ct)),
            _ => ProcessParticipationRules.Evaluate(access, await _context.RfqInvitations
                .Where(i => i.RfqProcessId == id && i.CompanyId == companyId).Select(i => (InvitationStatus?)i.Status).FirstOrDefaultAsync(ct)),
        };
        if (level != AuctionAccessLevel.Full)
            throw new ForbiddenAccessException(ProcessParticipationRules.QuotaMessage, ErrorCodes.QuotaExceeded);
        return facts;
    }
}

public sealed record GetProcessQuestionsQuery(ProcessKind Kind, Guid ProcessId) : IRequest<IReadOnlyList<ProcessQuestionDto>>;

public sealed class GetProcessQuestionsQueryHandler : IRequestHandler<GetProcessQuestionsQuery, IReadOnlyList<ProcessQuestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ProcessQuestionAccess _access;

    public GetProcessQuestionsQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes, ISupplierAccessService supplierAccess)
    {
        _context = context;
        _currentUser = currentUser;
        _access = new ProcessQuestionAccess(context, currentUser, scopes, supplierAccess);
    }

    public async Task<IReadOnlyList<ProcessQuestionDto>> Handle(GetProcessQuestionsQuery r, CancellationToken cancellationToken)
    {
        await _access.LoadAsync(r.Kind, r.ProcessId, cancellationToken);
        IQueryable<ProcessQuestion> query = _context.ProcessQuestions.AsNoTracking()
            .Where(q => q.ProcessKind == r.Kind && q.ProcessId == r.ProcessId);

        Guid? myCompany = _access.IsInternal ? null : _currentUser.CompanyId;
        if (myCompany is Guid mine)
        {
            query = query.Where(q => q.IsAnnouncement || q.CompanyId == mine || (q.IsPublic && q.Answer != null));
        }

        var rows = await query.OrderByDescending(q => q.CreatedAt)
            .Select(q => new { q.Id, q.Question, q.Answer, q.IsPublic, q.IsAnnouncement, q.CompanyId, CompanyName = q.Company != null ? q.Company.Name : null, q.CreatedAt, q.AnsweredAt })
            .ToListAsync(cancellationToken);

        return rows.Select(q => new ProcessQuestionDto(
            q.Id, q.Question, q.Answer, q.IsPublic, q.IsAnnouncement,
            // Tedarikçiye soran firma adı (kendisi dahil) dönmez; kendi sorusu IsMine ile işaretlenir.
            myCompany is null ? q.CompanyName : null,
            myCompany is Guid c && q.CompanyId == c,
            q.CreatedAt, q.AnsweredAt)).ToList();
    }
}

public sealed record AskProcessQuestionCommand(ProcessKind Kind, Guid ProcessId, string Question) : IRequest<Guid>;

[RequiresPermission(PermissionCodes.QaAnswer)]
public sealed record AnswerProcessQuestionCommand(Guid QuestionId, string Answer, bool IsPublic) : IRequest;

[RequiresPermission(PermissionCodes.QaAnswer)]
public sealed record PostProcessAnnouncementCommand(ProcessKind Kind, Guid ProcessId, string Text) : IRequest<Guid>;

public sealed class AskProcessQuestionCommandValidator : AbstractValidator<AskProcessQuestionCommand>
{
    public AskProcessQuestionCommandValidator() =>
        RuleFor(x => x.Question).NotEmpty().WithMessage("Soru boş olamaz.").MaximumLength(2000);
}

public sealed class AnswerProcessQuestionCommandValidator : AbstractValidator<AnswerProcessQuestionCommand>
{
    public AnswerProcessQuestionCommandValidator() =>
        RuleFor(x => x.Answer).NotEmpty().WithMessage("Cevap boş olamaz.").MaximumLength(4000);
}

public sealed class PostProcessAnnouncementCommandValidator : AbstractValidator<PostProcessAnnouncementCommand>
{
    public PostProcessAnnouncementCommandValidator() =>
        RuleFor(x => x.Text).NotEmpty().WithMessage("Duyuru metni boş olamaz.").MaximumLength(2000);
}

public sealed class ProcessQuestionCommandHandlers :
    IRequestHandler<AskProcessQuestionCommand, Guid>,
    IRequestHandler<AnswerProcessQuestionCommand>,
    IRequestHandler<PostProcessAnnouncementCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly INotificationRealtimeService _realtime;
    private readonly InternalNotificationAudience _audience;
    private readonly ProcessQuestionAccess _access;

    public ProcessQuestionCommandHandlers(
        IApplicationDbContext context, ICurrentUserService currentUser, IDataScopeService scopes, IDateTime dateTime,
        INotificationRealtimeService realtime, InternalNotificationAudience audience, ISupplierAccessService supplierAccess)
    {
        _context = context;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _realtime = realtime;
        _audience = audience;
        _access = new ProcessQuestionAccess(context, currentUser, scopes, supplierAccess);
    }

    public async Task<Guid> Handle(AskProcessQuestionCommand r, CancellationToken cancellationToken)
    {
        if (_access.IsInternal)
            throw new ForbiddenAccessException("Soru yalnızca tedarikçi tarafından sorulabilir; iç kullanıcılar duyuru yayınlar.");
        ProcessFacts p = await _access.LoadAsync(r.Kind, r.ProcessId, cancellationToken);
        if (!p.IsOpen)
            throw new ConflictException("Süreç soru almaya açık değil.", ErrorCodes.ProcessClosed);

        var question = new ProcessQuestion
        {
            ProcessKind = p.Kind,
            ProcessId = p.Id,
            CompanyId = _currentUser.CompanyId,
            AskedByUserId = _currentUser.UserId,
            Question = r.Question.Trim(),
        };
        _context.ProcessQuestions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        IReadOnlyList<Guid> audience = await _audience.ForScopeAsync(p.WorkspaceId, p.OwnerCompanyId, p.ResponsibleUserId, p.CreatedBy, cancellationToken);
        await NotificationCreator.CreateForInternalUsersAsync(_context, _realtime, _dateTime, audience, NotificationType.ProcessQuestion,
            "Yeni Tedarikçi Sorusu", $"\"{p.Title}\" {p.KindLabel} için yeni bir soru var.", p.RelatedType, p.Id, cancellationToken);
        return question.Id;
    }

    public async Task Handle(AnswerProcessQuestionCommand r, CancellationToken cancellationToken)
    {
        ProcessQuestion q = await _context.ProcessQuestions.FirstOrDefaultAsync(x => x.Id == r.QuestionId && !x.IsAnnouncement, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessQuestion), r.QuestionId);
        ProcessFacts p = await _access.LoadAsync(q.ProcessKind, q.ProcessId, cancellationToken);

        bool firstAnswer = q.Answer is null;
        bool becamePublic = r.IsPublic && !q.IsPublic;
        q.Answer = r.Answer.Trim();
        q.IsPublic = r.IsPublic;
        q.AnsweredByUserId = _currentUser.UserId;
        q.AnsweredAt = _dateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        if (q.CompanyId is Guid asker && firstAnswer)
        {
            await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, asker, NotificationType.ProcessQuestion,
                "Sorunuz Cevaplandı", $"\"{p.Title}\" {p.KindLabel} için sorduğunuz soru cevaplandı.", p.RelatedType, p.Id, cancellationToken);
        }

        if (becamePublic || (firstAnswer && r.IsPublic))
        {
            foreach (Guid company in p.ParticipantCompanyIds.Where(c => c != q.CompanyId))
            {
                await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, company, NotificationType.ProcessQuestion,
                    "Yeni Soru-Cevap", $"\"{p.Title}\" {p.KindLabel} için herkese açık yeni bir soru-cevap yayınlandı.", p.RelatedType, p.Id, cancellationToken);
            }
        }
    }

    public async Task<Guid> Handle(PostProcessAnnouncementCommand r, CancellationToken cancellationToken)
    {
        if (!_access.IsInternal)
            throw new ForbiddenAccessException("Duyuruyu yalnızca süreç sahibi yayınlayabilir.");
        ProcessFacts p = await _access.LoadAsync(r.Kind, r.ProcessId, cancellationToken);

        var announcement = new ProcessQuestion
        {
            ProcessKind = p.Kind,
            ProcessId = p.Id,
            AskedByUserId = _currentUser.UserId,
            Question = r.Text.Trim(),
            IsAnnouncement = true,
            IsPublic = true,
        };
        _context.ProcessQuestions.Add(announcement);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (Guid company in p.ParticipantCompanyIds)
        {
            await NotificationCreator.CreateForCompanyAsync(_context, _realtime, _dateTime, company, NotificationType.ProcessQuestion,
                "Yeni Duyuru", $"\"{p.Title}\" {p.KindLabel} için yeni bir duyuru yayınlandı.", p.RelatedType, p.Id, cancellationToken);
        }
        return announcement.Id;
    }
}
