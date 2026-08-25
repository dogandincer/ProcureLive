using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Application.Common.Security;

/// <summary>
/// Implemented by MediatR commands/queries that need resource-based authorization
/// (e.g. a Supplier user may only load their own Company). AuthorizationBehavior
/// picks this up automatically and calls IAuthorizationService with the loaded resource.
/// </summary>
public interface IRequireResourceAuthorization
{
    string PolicyName { get; }

    Task<object?> LoadResourceAsync(IApplicationDbContext context, CancellationToken cancellationToken);
}
