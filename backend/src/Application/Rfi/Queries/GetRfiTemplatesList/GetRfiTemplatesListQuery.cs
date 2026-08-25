using MediatR;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiTemplatesList;

public sealed record GetRfiTemplatesListQuery : IRequest<IReadOnlyCollection<RfiTemplateDto>>;
