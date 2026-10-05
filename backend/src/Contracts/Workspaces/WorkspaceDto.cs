namespace ProcureLive.Contracts.Workspaces;

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int CompanyCount,
    int ProductCount);

public sealed record CreateWorkspaceRequest(string Name, string? Description);

public sealed record UpdateWorkspaceRequest(string Name, string? Description, bool IsActive);
