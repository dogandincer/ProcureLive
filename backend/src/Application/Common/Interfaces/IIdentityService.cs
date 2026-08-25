using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<IdentityOperationResult> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        Guid? companyId,
        CancellationToken cancellationToken);

    Task<ApplicationUserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<ApplicationUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    Task UpdateLastLoginAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> UpdateProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        Guid? companyId,
        CancellationToken cancellationToken);

    Task<bool> SetRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    Task<PagedResult<ApplicationUserInfo>> ListUsersAsync(
        string? searchTerm,
        Guid? companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
