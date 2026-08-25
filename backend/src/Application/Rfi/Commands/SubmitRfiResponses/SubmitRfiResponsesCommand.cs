using MediatR;

namespace ProcureLive.Application.Rfi.Commands.SubmitRfiResponses;

public sealed record RfiAnswerInput(
    Guid RfiQuestionId,
    string? AnswerText,
    decimal? AnswerNumber,
    bool? AnswerBool,
    Guid? SelectedOptionId,
    string? AttachmentUrl);

public sealed record SubmitRfiResponsesCommand(Guid RfiProcessId, IReadOnlyCollection<RfiAnswerInput> Answers) : IRequest;
