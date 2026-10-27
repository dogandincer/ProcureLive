using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Companies.Commands.AddAddress;

public sealed record AddAddressCommand(
    Guid CompanyId,
    AddressType AddressType,
    string Country,
    string City,
    string? District,
    string? PostalCode,
    string Line1,
    string? Line2) : IRequest<Guid>, ICompanyScopedRequest;
