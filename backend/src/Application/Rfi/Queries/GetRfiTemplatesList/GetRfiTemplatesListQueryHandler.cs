using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiTemplatesList;

public sealed class GetRfiTemplatesListQueryHandler
    : IRequestHandler<GetRfiTemplatesListQuery, IReadOnlyCollection<RfiTemplateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetRfiTemplatesListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<RfiTemplateDto>> Handle(GetRfiTemplatesListQuery request, CancellationToken cancellationToken)
    {
        List<RfiTemplate> templates = await _context.RfiTemplates
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        return templates.Select(template => new RfiTemplateDto(
            template.Id, template.Name, template.Description, template.CategoryId, template.IsActive,
            template.Questions.OrderBy(q => q.SortOrder).Select(q => new RfiQuestionDto(
                q.Id, q.QuestionText, q.QuestionType.ToString(), q.IsRequired, q.SortOrder, q.Weight,
                q.Options.Select(o => new RfiQuestionOptionDto(o.Id, o.OptionText, o.ScoreValue)).ToList())).ToList()
            )).ToList();
    }
}
