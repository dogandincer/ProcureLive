namespace ProcureLive.Contracts.Workspaces;

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int CompanyCount,
    int ProductCount,
    Guid? FirmId = null,
    string? FirmCode = null,
    string? FirmName = null,
    bool IsDefault = false);

/// <summary>FirmId zorunludur (sarı alan: firma seçimi). IsDefault işaretlenirse firmanın diğer işyerlerinden kaldırılır.</summary>
public sealed record CreateWorkspaceRequest(string Name, string? Description, Guid FirmId, bool IsDefault = false);

public sealed record UpdateWorkspaceRequest(string Name, string? Description, bool IsActive, Guid FirmId, bool IsDefault = false);
