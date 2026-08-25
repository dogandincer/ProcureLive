using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.SubmitRfiResponses;

public sealed class SubmitRfiResponsesCommandHandler : IRequestHandler<SubmitRfiResponsesCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SubmitRfiResponsesCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(SubmitRfiResponsesCommand request, CancellationToken cancellationToken)
    {
        Guid? companyId = _currentUserService.CompanyId;

        if (companyId is null)
        {
            throw new ForbiddenAccessException("Kullanıcının bağlı olduğu bir Cari bulunamadı.");
        }

        RfiInvitation? invitation = await _context.RfiInvitations
            .FirstOrDefaultAsync(i => i.RfiProcessId == request.RfiProcessId && i.CompanyId == companyId.Value, cancellationToken);

        bool canRespond = invitation is not null
            && (invitation.Status == InvitationStatus.Accepted || invitation.Status == InvitationStatus.Participating);

        if (!canRespond)
        {
            throw new ForbiddenAccessException("Bu RFI sürecine yanıt vermek için davetin kabul edilmiş olması gerekir.");
        }

        List<Guid> validQuestionIds = await _context.RfiQuestions
            .Where(q => q.RfiTemplateId == _context.RfiProcesses.Where(p => p.Id == request.RfiProcessId).Select(p => p.RfiTemplateId).FirstOrDefault())
            .Select(q => q.Id)
            .ToListAsync(cancellationToken);

        List<RfiResponse> existingResponses = await _context.RfiResponses
            .Where(r => r.RfiProcessId == request.RfiProcessId && r.CompanyId == companyId.Value)
            .ToListAsync(cancellationToken);

        foreach (var answer in request.Answers)
        {
            if (!validQuestionIds.Contains(answer.RfiQuestionId))
            {
                throw new ConflictException("Yanıtlanan soru bu RFI şablonuna ait değil.");
            }

            RfiResponse? existing = existingResponses.FirstOrDefault(r => r.RfiQuestionId == answer.RfiQuestionId);

            if (existing is not null)
            {
                existing.AnswerText = answer.AnswerText;
                existing.AnswerNumber = answer.AnswerNumber;
                existing.AnswerBool = answer.AnswerBool;
                existing.SelectedOptionId = answer.SelectedOptionId;
                existing.AttachmentUrl = answer.AttachmentUrl;
            }
            else
            {
                _context.RfiResponses.Add(new RfiResponse
                {
                    RfiProcessId = request.RfiProcessId,
                    RfiQuestionId = answer.RfiQuestionId,
                    CompanyId = companyId.Value,
                    AnswerText = answer.AnswerText,
                    AnswerNumber = answer.AnswerNumber,
                    AnswerBool = answer.AnswerBool,
                    SelectedOptionId = answer.SelectedOptionId,
                    AttachmentUrl = answer.AttachmentUrl
                });
            }
        }

        if (invitation!.Status == InvitationStatus.Accepted)
        {
            invitation.Status = InvitationStatus.Participating;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
