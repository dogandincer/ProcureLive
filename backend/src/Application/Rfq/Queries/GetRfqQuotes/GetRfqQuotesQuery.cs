using MediatR;
using ProcureLive.Contracts.Rfq;

namespace ProcureLive.Application.Rfq.Queries.GetRfqQuotes;

/// <summary>Admin/Buyer: all quotes for the process. Supplier: only their own (enforced in handler).</summary>
public sealed record GetRfqQuotesQuery(Guid RfqProcessId) : IRequest<IReadOnlyCollection<RfqQuoteDto>>;
