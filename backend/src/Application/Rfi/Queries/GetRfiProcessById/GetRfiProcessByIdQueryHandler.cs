using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessById;

public sealed class GetRfiProcessByIdQueryHandler : IRequestHandler<GetRfiProcessByIdQuery, RfiProcessDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetRfiProcessByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
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

        List<RfiInvitationDto> invitations = process.Invitations
            .Select(i => new RfiInvitationDto(i.Id, i.CompanyId, i.Company.Name, i.Status.ToString(), i.SentAt, i.RespondedAt))
            .ToList();

        return new RfiProcessDetailDto(
            process.Id, process.RfiTemplateId, process.RfiTemplate.Name, process.Title,
            process.OwnerCompanyId, process.OwnerCompany.Name, process.Status.ToString(), process.DueDate,
            questions, invitations);
    }
}
