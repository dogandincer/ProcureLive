using ProcureLive.Domain.Common;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Domain.Entities.Rfi;

public class RfiQuestion : BaseEntity
{
    public Guid RfiTemplateId { get; set; }

    public RfiTemplate RfiTemplate { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;

    public RfiQuestionType QuestionType { get; set; }

    public bool IsRequired { get; set; }

    public int SortOrder { get; set; }

    public decimal Weight { get; set; } = 1m;

    public ICollection<RfiQuestionOption> Options { get; set; } = new List<RfiQuestionOption>();
}
