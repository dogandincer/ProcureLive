using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.AddRfiQuestion;

[RequiresPermission(PermissionCodes.RfiManage)]
public sealed record AddRfiQuestionCommand(
    Guid RfiTemplateId,
    string QuestionText,
    RfiQuestionType QuestionType,
    bool IsRequired,
    int SortOrder,
    decimal Weight,
    IReadOnlyCollection<string>? Options) : IRequest<Guid>;
