using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Rfi;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Rfi;

public sealed class RfiTemplateConfiguration : IEntityTypeConfiguration<RfiTemplate>
{
    public void Configure(EntityTypeBuilder<RfiTemplate> builder)
    {
        builder.ToTable("RfiTemplates");
        builder.Property(t => t.Name).HasMaxLength(250).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(2000);

        builder.HasMany(t => t.Questions)
            .WithOne(q => q.RfiTemplate)
            .HasForeignKey(q => q.RfiTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
