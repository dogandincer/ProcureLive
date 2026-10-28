using MediatR;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiProcessesList;

public sealed record GetRfiProcessesListQuery(string? Status, int Page, int PageSize, Guid? FirmId = null, Guid? WorkspaceId = null, string? Search = null)
    : IRequest<PagedResult<RfiProcessDto>>;
