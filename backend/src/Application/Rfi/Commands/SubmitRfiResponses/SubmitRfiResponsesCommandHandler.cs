using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Rfi;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.SubmitRfiResponses;

public sealed class SubmitRfiResponsesCommandHandler : IRequestHandler<SubmitRfiResponsesCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISupplierAccessService _accessService;

    public SubmitRfiResponsesCommandHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, ISupplierAccessService accessService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _accessService = accessService;
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

        ProcessStatus status = await _context.RfiProcesses
            .Where(p => p.Id == request.RfiProcessId).Select(p => p.Status).FirstAsync(cancellationToken);
        if (!ProcessParticipationRules.IsOpen(status))
        {
            throw new ConflictException(ProcessParticipationRules.ClosedMessage, ErrorCodes.ProcessClosed);
        }

        var questions = await _context.RfiQuestions
            .Where(q => q.RfiTemplateId == _context.RfiProcesses.Where(p => p.Id == request.RfiProcessId).Select(p => p.RfiTemplateId).FirstOrDefault())
            .Select(q => new { q.Id, q.IsRequired, q.QuestionType, q.QuestionText })
            .ToListAsync(cancellationToken);
        List<Guid> validQuestionIds = questions.Select(q => q.Id).ToList();

        // Zorunlu sorular boş geçilemez (soru tipine göre ilgili alan dolu olmalı).
        List<string> missing = questions.Where(q => q.IsRequired).Where(q =>
        {
            var a = request.Answers.FirstOrDefault(x => x.RfiQuestionId == q.Id);
            return a is null || q.QuestionType switch
            {
                RfiQuestionType.Number => a.AnswerNumber is null,
                RfiQuestionType.Boolean => a.AnswerBool is null,
                RfiQuestionType.MultipleChoice => a.SelectedOptionId is null,
                RfiQuestionType.FileUpload => string.IsNullOrWhiteSpace(a.AttachmentUrl),
                _ => string.IsNullOrWhiteSpace(a.AnswerText),
            };
        }).Select(q => q.QuestionText).ToList();
        if (missing.Count > 0)
        {
            throw new ConflictException($"Zorunlu sorular yanıtlanmadı: {string.Join(", ", missing)}", ErrorCodes.RequiredAnswersMissing);
        }

        List<RfiResponse> existingResponses = await _context.RfiResponses
            .Where(r => r.RfiProcessId == request.RfiProcessId && r.CompanyId == companyId.Value)
            .ToListAsync(cancellationToken);

        // Gönderilmiş yanıtın revizyonu Plus'a özel.
        if (existingResponses.Count > 0 && !(await _accessService.GetContextAsync(companyId.Value, cancellationToken)).IsPlus)
        {
            throw new ConflictException(ProcessParticipationRules.RevisionMessage, ErrorCodes.PlusRequired);
        }

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

        // İlk cevap gelince süreç "Devam ediyor"a geçer.
        if (status == ProcessStatus.Sent)
        {
            RfiProcess process = await _context.RfiProcesses.FirstAsync(p => p.Id == request.RfiProcessId, cancellationToken);
            process.Status = ProcessStatus.InProgress;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
