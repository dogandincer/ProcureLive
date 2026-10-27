using MediatR;
using ProcureLive.Contracts.Workspaces;

namespace ProcureLive.Application.Workspaces.Queries.GetWorkspacesList;

/// <summary>FirmId verilirse yalnızca o firmanın işyerleri (başlıktaki firma seçimi).</summary>
public sealed record GetWorkspacesListQuery(bool? ActiveOnly, Guid? FirmId = null) : IRequest<List<WorkspaceDto>>;
