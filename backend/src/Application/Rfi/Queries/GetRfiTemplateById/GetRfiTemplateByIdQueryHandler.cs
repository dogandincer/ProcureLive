using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Rfi;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiTemplateById;

public sealed class GetRfiTemplateByIdQueryHandler : IRequestHandler<GetRfiTemplateByIdQuery, RfiTemplateDto>
{
    private readonly IApplicationDbContext _context;

    public GetRfiTemplateByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RfiTemplateDto> Handle(GetRfiTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        RfiTemplate? template = await _context.RfiTemplates
            .Include(t => t.Questions).ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (template is null)
        {
            throw new NotFoundException(nameof(RfiTemplate), request.Id);
        }

        List<RfiQuestionDto> questions = template.Questions
            .OrderBy(q => q.SortOrder)
            .Select(q => new RfiQuestionDto(
                q.Id, q.QuestionText, q.QuestionType.ToString(), q.IsRequired, q.SortOrder, q.Weight,
                q.Options.Select(o => new RfiQuestionOptionDto(o.Id, o.OptionText, o.ScoreValue)).ToList()))
            .ToList();

        return new RfiTemplateDto(template.Id, template.Name, template.Description, template.CategoryId, template.IsActive, questions);
    }
}
