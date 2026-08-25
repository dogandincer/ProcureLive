using MediatR;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Rfi.Commands.AddRfiQuestion;

public sealed record AddRfiQuestionCommand(
    Guid RfiTemplateId,
    string QuestionText,
    RfiQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    decimal Weight,
    IReadOnlyCollection<string>? Options) : IRequest<Guid>;
