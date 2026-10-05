namespace ProcureLive.Contracts.Companies;

public sealed record CreateSectorRequest(string Name, string? Description);

public sealed record UpdateSectorRequest(string Name, string? Description, bool IsActive);

public sealed record AssignSectorsRequest(IReadOnlyCollection<Guid> SectorIds);
