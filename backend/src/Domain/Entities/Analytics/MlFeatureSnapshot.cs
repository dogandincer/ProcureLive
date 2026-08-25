using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Analytics;

/// <summary>
/// Phase-4 feature store seed: periodic snapshots of engineered features per entity,
/// consumed later by the AI "recommended auction model" endpoint.
/// </summary>
public class MlFeatureSnapshot : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public DateTime SnapshotDate { get; set; }

    /// <summary>JSON-serialized feature bag (jsonb in Postgres).</summary>
    public string FeaturesJson { get; set; } = "{}";
}
