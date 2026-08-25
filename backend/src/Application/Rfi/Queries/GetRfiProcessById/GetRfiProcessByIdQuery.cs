using MediatR;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessById;

public sealed record GetRfiProcessByIdQuery(Guid Id) : IRequest<RfiProcessDetailDto>;
