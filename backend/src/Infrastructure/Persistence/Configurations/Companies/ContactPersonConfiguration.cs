using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Infrastructure.Persistence.Configurations.Companies;

public sealed class ContactPersonConfiguration : IEntityTypeConfiguration<ContactPerson>
{
    public void Configure(EntityTypeBuilder<ContactPerson> builder)
    {
        builder.ToTable("ContactPersons");

        builder.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Title).HasMaxLength(150);
        builder.Property(c => c.Email).HasMaxLength(250).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30);

        builder.HasIndex(c => c.CompanyId);
    }
}
