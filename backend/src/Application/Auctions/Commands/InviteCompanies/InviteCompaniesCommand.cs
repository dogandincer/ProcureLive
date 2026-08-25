using MediatR;

namespace ProcureLive.Application.Auctions.Commands.InviteCompanies;

public sealed record InviteCompaniesCommand(Guid AuctionId, IReadOnlyCollection<Guid> CompanyIds) : IRequest;
