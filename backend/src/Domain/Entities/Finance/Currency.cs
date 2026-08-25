using ProcureLive.Domain.Common;

namespace ProcureLive.Domain.Entities.Finance;

public class Currency : AuditableEntity
{
    /// <summary>ISO 4217 code, e.g. "TRY", "USD", "EUR".</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public bool IsBaseCurrency { get; set; }

    public ICollection<ExchangeRate> ExchangeRates { get; set; } = new List<ExchangeRate>();
}
