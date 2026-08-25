using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Rfi;

public class RfiQuestionOption : BaseEntity
{
    public Guid RfiQuestionId { get; set; }

    public RfiQuestion RfiQuestion { get; set; } = null!;

    public string OptionText { get; set; } = string.Empty;

    public decimal ScoreValue { get; set; }
}
