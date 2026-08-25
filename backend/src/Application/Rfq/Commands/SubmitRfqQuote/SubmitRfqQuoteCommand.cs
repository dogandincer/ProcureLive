using MediatR;

namespace ProcureLive.Application.Rfq.Commands.SubmitRfqQuote;

public sealed record RfqQuoteLineItemInput(Guid RfqLineItemId, decimal UnitPrice, Guid CurrencyId, int? LeadTimeDays, string? Notes);

/// <summary>
/// One process, multiple line items, each priced in whatever currency the supplier chooses —
/// "tek bir süreç içerisinde farklı talepler için çoklu para biriminde teklif toplayabilme."
/// </summary>
public sealed record SubmitRfqQuoteCommand(
    Guid RfqProcessId,
    DateTime? ValidUntil,
    string? Notes,
    IReadOnlyCollection<RfqQuoteLineItemInput> LineItems) : IRequest<Guid>;
