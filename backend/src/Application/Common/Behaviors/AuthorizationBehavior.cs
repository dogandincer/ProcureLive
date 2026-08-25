using MediatR;
using Microsoft.AspNetCore.Authorization;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Common.Behaviors;

/// <summary>
/// Centralized resource-based authorization: any request implementing
/// IRequireResourceAuthorization is checked against IAuthorizationService automatically,
/// so handlers never repeat manual ownership checks.
/// </summary>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationService _authorizationService;

    public AuthorizationBehavior(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _authorizationService = authorizationService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequireResourceAuthorization authRequest)
        {
            object? resource = await authRequest.LoadResourceAsync(_context, cancellationToken);

            if (resource is not null && _currentUserService.Principal is not null)
            {
                AuthorizationResult result = await _authorizationService.AuthorizeAsync(
                    _currentUserService.Principal,
                    resource,
                    authRequest.PolicyName);

                if (!result.Succeeded)
                {
                    throw new ForbiddenAccessException();
                }
            }
        }

        return await next();
    }
}
