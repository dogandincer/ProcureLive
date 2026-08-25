using MediatR;
using ProcureLive.Contracts.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqProcessById;

public sealed record GetRfqProcessByIdQuery(Guid Id) : IRequest<RfqProcessDetailDto>;
