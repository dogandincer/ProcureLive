using MediatR;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Rfi.Commands.EvaluateSupplier;

public sealed record EvaluateSupplierCommand(Guid RfiProcessId, Guid CompanyId, decimal TotalScore, string? Comments) : IRequest<Guid>, IRfiScopedRequest;
