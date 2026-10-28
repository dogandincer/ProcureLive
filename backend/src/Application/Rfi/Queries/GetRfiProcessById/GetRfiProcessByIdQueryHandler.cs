using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessById;

public sealed class GetRfiProcessByIdQueryHandler : IRequestHandler<GetRfiProcessByIdQuery, RfiProcessDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ISupplierAccessService _accessService;

    public GetRfiProcessByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser, ISupplierAccessService accessService)
    {
        _context = context;
        _currentUser = currentUser;
        _accessService = accessService;
    }

    public async Task<RfiProcessDetailDto> Handle(GetRfiProcessByIdQuery request, CancellationToken cancellationToken)
    {
        RfiProcess? process = await _context.RfiProcesses
            .Include(p => p.OwnerCompany)
            .Include(p => p.RfiTemplate).ThenInclude(t => t.Questions).ThenInclude(q => q.Options)
            .Include(p => p.Invitations).ThenInclude(i => i.Company)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (process is null)
        {
            throw new NotFoundException(nameof(RfiProcess), request.Id);
        }

        List<RfiQuestionDto> questions = process.RfiTemplate.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q => new RfiQuestionDto(
                q.Id, q.QuestionText, q.QuestionType.ToString(), q.IsRequired, q.SortOrder, q.Weight,
                q.Options.Select(o => new RfiQuestionOptionDto(o.Id, o.OptionText, o.ScoreValue)).ToList()))
            .ToList();

        bool isInternalUser = _currentUser.IsInRole(Roles.Admin) || _currentUser.IsInRole(Roles.Buyer);
        Guid? viewerCompanyId = _currentUser.CompanyId;

        // Tedarikçi rakip firmaların kimliğini görmez: yalnızca kendi davet kaydı döner.
        List<RfiInvitationDto> invitations = process.Invitations
            .Where(i => isInternalUser || i.CompanyId == viewerCompanyId)
            .Select(i => new RfiInvitationDto(i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.SentAt, i.RespondedAt))
            .ToList();

        bool isOpen = ProcessParticipationRules.IsOpen(process.Status);

        if (isInternalUser || viewerCompanyId is not Guid companyId)
        {
            string? workspaceName = process.WorkspaceId is Guid ws
                ? await _context.Workspaces.Where(w => w.Id == ws).Select(w => w.Name).FirstOrDefaultAsync(cancellationToken)
                : null;
            return new RfiProcessDetailDto(
                process.Id, process.RfiTemplateId, process.RfiTemplate.Name, process.Title,
                process.OwnerCompanyId, process.OwnerCompany.Name, process.Status.ToString(), process.DueDate,
                questions, invitations, IsOpen: isOpen, WorkspaceId: process.WorkspaceId, WorkspaceName: workspaceName);
        }

        SupplierAccessContext access = await _accessService.GetContextAsync(companyId, cancellationToken);
        InvitationStatus? myStatus = process.Invitations.FirstOrDefault(i => i.CompanyId == companyId)?.Status;
        AuctionAccessLevel level = ProcessParticipationRules.Evaluate(access, myStatus);

        // Kota dolu ve davet kabul edilmemiş: sorular sunucuda düşürülür, arayüz "Plus alınız" gösterir.
        if (level != AuctionAccessLevel.Full)
        {
            return new RfiProcessDetailDto(
                process.Id, process.RfiTemplateId, process.RfiTemplate.Name, process.Title,
                process.OwnerCompanyId, process.OwnerCompany.Name, process.Status.ToString(), process.DueDate,
                Array.Empty<RfiQuestionDto>(), invitations, level.ToString(), isOpen);
        }

        List<RfiMyAnswerDto> myAnswers = await _context.RfiResponses
            .Where(r => r.RfiProcessId == process.Id && r.CompanyId == companyId)
            .Select(r => new RfiMyAnswerDto(r.RfiQuestionId, r.AnswerText, r.AnswerNumber, r.AnswerBool, r.SelectedOptionId, r.AttachmentUrl))
            .ToListAsync(cancellationToken);
        bool hasSubmitted = myAnswers.Count > 0;

        return new RfiProcessDetailDto(
            process.Id, process.RfiTemplateId, process.RfiTemplate.Name, process.Title,
            process.OwnerCompanyId, process.OwnerCompany.Name, process.Status.ToString(), process.DueDate,
            questions, invitations, level.ToString(), isOpen, hasSubmitted,
            ProcessParticipationRules.CanSubmit(access, process.Status, myStatus, hasSubmitted), myAnswers);
    }
}
