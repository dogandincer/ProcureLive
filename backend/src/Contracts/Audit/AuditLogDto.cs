namespace ProcureLive.Contracts.Audit;

public sealed record AuditLogDto(
    Guid Id,
    string EntityName,
    Guid EntityId,
    string Action,
    Guid? ChangedByUserId,
    string? ChangedByUserName,
    DateTime ChangedAt,
    string? OldValues,
    string? NewValues,
    string? AffectedColumns);
