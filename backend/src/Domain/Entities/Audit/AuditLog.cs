using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Audit;

public class AuditLog : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public AuditAction Action { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public string? ChangedByUserName { get; set; }

    public DateTime ChangedAt { get; set; }

    /// <summary>JSON-serialized old property values (jsonb in Postgres).</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON-serialized new property values (jsonb in Postgres).</summary>
    public string? NewValues { get; set; }

    /// <summary>JSON array of property names that changed (jsonb in Postgres).</summary>
    public string? AffectedColumns { get; set; }

    public string? IpAddress { get; set; }

    public string? CorrelationId { get; set; }
}
