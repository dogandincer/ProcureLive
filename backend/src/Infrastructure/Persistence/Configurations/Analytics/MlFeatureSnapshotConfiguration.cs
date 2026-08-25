using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Analytics;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Analytics;

public sealed class MlFeatureSnapshotConfiguration : IEntityTypeConfiguration<MlFeatureSnapshot>
{
    public void Configure(EntityTypeBuilder<MlFeatureSnapshot> builder)
    {
        builder.ToTable("MlFeatureSnapshots");
        builder.Property(s => s.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(s => s.FeaturesJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(s => new { s.EntityType, s.EntityId, s.SnapshotDate });
    }
}
