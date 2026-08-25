using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqProcessesList;

public sealed record GetRfqProcessesListQuery(string? Status, int Page, int PageSize) : IRequest<PagedResult<RfqProcessDto>>;
