using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.InviteCompanies;

public sealed record InviteCompaniesCommand(Guid AuctionId, IReadOnlyCollection<Guid> CompanyIds) : IRequest, IAuctionScopedRequest;
