using MediatR;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiTemplateById;

public sealed record GetRfiTemplateByIdQuery(Guid Id) : IRequest<RfiTemplateDto>;
