using MediatR;
using ProcureLive.Contracts.Rfi;

namespace ProcureLive.Application.Rfi.Queries.GetRfiResponses;

/// <summary>CompanyId null -> all companies' responses (admin evaluation screen); otherwise one company's.</summary>
public sealed record GetRfiResponsesQuery(Guid RfiProcessId, Guid? CompanyId) : IRequest<IReadOnlyCollection<RfiResponseDto>>;
