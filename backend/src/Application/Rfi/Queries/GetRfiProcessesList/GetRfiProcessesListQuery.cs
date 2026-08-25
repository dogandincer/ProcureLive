using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessesList;

public sealed record GetRfiProcessesListQuery(string? Status, int Page, int PageSize) : IRequest<PagedResult<RfiProcessDto>>;
