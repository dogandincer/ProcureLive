using MediatR;

namespace ProcureLive.Application.Rfq.Commands.CreateRfqProcess;

/// <summary>RFQ setup step 1/4: basic info. Steps 2-4 are AddLineItem(s), InviteCompanies, Publish.</summary>
public sealed record CreateRfqProcessCommand(string Title, Guid OwnerCompanyId, Guid CurrencyId, DateTime DueDate) : IRequest<Guid>;
