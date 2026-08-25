using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Finance;

public class ExchangeRate : AuditableEntity
{
    public Guid CurrencyId { get; set; }

    public Currency Currency { get; set; } = null!;

    public DateOnly RateDate { get; set; }

    /// <summary>Rate to convert 1 unit of Currency into the system base currency.</summary>
    public decimal RateToBase { get; set; }

    public string? Source { get; set; }
}
