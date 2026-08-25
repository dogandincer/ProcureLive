using ProcureLive.Domain.Common;
using ProcureLive.Domain.Entities.Companies;

namespace ProcureLive.Domain.Entities.Rfi;

public class RfiResponse : AuditableEntity
{
    public Guid RfiProcessId { get; set; }

    public RfiProcess RfiProcess { get; set; } = null!;

    public Guid RfiQuestionId { get; set; }

    public RfiQuestion RfiQuestion { get; set; } = null!;

    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public string? AnswerText { get; set; }

    public decimal? AnswerNumber { get; set; }

    public bool? AnswerBool { get; set; }

    public Guid? SelectedOptionId { get; set; }

    public RfiQuestionOption? SelectedOption { get; set; }

    public string? AttachmentUrl { get; set; }
}
