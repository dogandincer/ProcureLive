using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Companies;

public class ContactPerson : AuditableEntity
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public bool IsPrimary { get; set; }
}
