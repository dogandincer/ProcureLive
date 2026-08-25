using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Application.Rfi.Commands.AddRfiQuestion;

public sealed class AddRfiQuestionCommandHandler : IRequestHandler<AddRfiQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public AddRfiQuestionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddRfiQuestionCommand request, CancellationToken cancellationToken)
    {
        bool templateExists = await _context.RfiTemplates.AnyAsync(t => t.Id == request.RfiTemplateId, cancellationToken);

        if (!templateExists)
        {
            throw new NotFoundException(nameof(RfiTemplate), request.RfiTemplateId);
        }

        var question = new RfiQuestion
        {
            RfiTemplateId = request.RfiTemplateId,
            QuestionText = request.QuestionText,
            QuestionType = request.QuestionType,
            IsRequired = request.IsRequired,
            SortOrder = request.SortOrder,
            Weight = request.Weight
        };

        _context.RfiQuestions.Add(question);

        if (request.Options is not null)
        {
            foreach (string optionText in request.Options)
            {
                _context.RfiQuestionOptions.Add(new RfiQuestionOption
                {
                    RfiQuestion = question,
                    OptionText = optionText,
                    ScoreValue = 0
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return question.Id;
    }
}
