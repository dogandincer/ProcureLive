using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiResponses;

public sealed class GetRfiResponsesQueryHandler : IRequestHandler<GetRfiResponsesQuery, IReadOnlyCollection<RfiResponseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRfiResponsesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<RfiResponseDto>> Handle(GetRfiResponsesQuery request, CancellationToken cancellationToken)
    {
        IQueryable<RfiResponse> query = _context.RfiResponses
            .Include(r => r.RfiQuestion)
            .Include(r => r.Company)
            .Include(r => r.SelectedOption)
            .Where(r => r.RfiProcessId == request.RfiProcessId);

        if (request.CompanyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == request.CompanyId.Value);
        }

        List<RfiResponse> responses = await query
            .OrderBy(r => r.RfiQuestion.SortOrder)
            .ToListAsync(cancellationToken);

        return responses.Select(r => new RfiResponseDto(
            r.RfiQuestionId, r.RfiQuestion.QuestionText, r.CompanyId, r.Company.Name,
            r.AnswerText, r.AnswerNumber, r.AnswerBool, r.SelectedOption?.OptionText, r.AttachmentUrl)).ToList();
    }
}
