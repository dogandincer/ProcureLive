using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfq.Commands.AddRfqLineItem;

public sealed record AddRfqLineItemCommand(
    Guid RfqProcessId,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    UnitOfMeasure UnitOfMeasure,
    int SortOrder) : IRequest<Guid>, IRfqScopedRequest;
