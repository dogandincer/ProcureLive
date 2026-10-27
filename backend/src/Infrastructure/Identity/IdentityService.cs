using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Models;
using ProcureLive.Contracts.Common;

namespace ProcureLive.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IdentityOperationResult> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CompanyId = companyId,
            IsActive = true
        };

        IdentityResult createResult = await _userManager.CreateAsync(user, password);

        if (!createResult.Succeeded)
        {
            return IdentityOperationResult.Failure(createResult.Errors.Select(e => e.Description).ToArray());
        }

        IdentityResult roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            return IdentityOperationResult.Failure(roleResult.Errors.Select(e => e.Description).ToArray());
        }

        return IdentityOperationResult.Success(user.Id);
    }

    public async Task<ApplicationUserInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        return user is null ? null : await MapToInfoAsync(user);
    }

    public async Task<ApplicationUserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await MapToInfoAsync(user);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());
        return user is not null && await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task UpdateLastLoginAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return;
        }

        user.LastLoginAt = utcNow;
        await _userManager.UpdateAsync(user);
    }

    public async Task<bool> UpdateProfileAsync(
        Guid userId,
        string firstName,
        string lastName,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return false;
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.CompanyId = companyId;

        IdentityResult result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> SetRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return false;
        }

        IList<string> currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        IdentityResult result = await _userManager.AddToRolesAsync(user, roles);

        return result.Succeeded;
    }

    public async Task<bool> RegisterFailedLoginAsync(Guid userId, DateTime utcNow, int maxFailedAttempts, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return false;
        }

        user.AccessFailedCount++;
        bool deactivate = user.AccessFailedCount > maxFailedAttempts;

        if (deactivate && await _userManager.IsInRoleAsync(user, Application.Common.Security.Roles.Admin))
        {
            // Son aktif Admin kilitlenirse kimse hesapları yeniden açamaz: bu durumda yalnızca sayaç tutulur.
            IList<ApplicationUser> admins = await _userManager.GetUsersInRoleAsync(Application.Common.Security.Roles.Admin);
            deactivate = admins.Count(a => a.IsActive && a.Id != user.Id) > 0;
        }

        if (deactivate)
        {
            user.IsActive = false;
            user.LockedOutAt = utcNow;
            user.AccessFailedCount = 0;
        }

        await _userManager.UpdateAsync(user);
        return deactivate;
    }

    public async Task ResetFailedLoginsAsync(Guid userId, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is not null && user.AccessFailedCount != 0)
        {
            user.AccessFailedCount = 0;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken)
    {
        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        if (isActive)
        {
            // Admin yeniden aktifleştirince hatalı giriş sayacı ve kilit kaydı sıfırlanır.
            user.LockedOutAt = null;
            user.AccessFailedCount = 0;
        }

        IdentityResult result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<PagedResult<ApplicationUserInfo>> ListUsersAsync(
        string? searchTerm,
        Guid? companyId,
        int page,
        int pageSize,
        IReadOnlyCollection<string>? roles,
        CancellationToken cancellationToken)
    {
        IQueryable<ApplicationUser> query = _userManager.Users.AsQueryable();

        if (roles is { Count: > 0 })
        {
            var ids = new HashSet<Guid>();
            foreach (string role in roles)
            {
                foreach (ApplicationUser u in await _userManager.GetUsersInRoleAsync(role))
                {
                    ids.Add(u.Id);
                }
            }

            query = query.Where(u => ids.Contains(u.Id));
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim();
            query = query.Where(u =>
                u.Email!.Contains(term) || u.FirstName.Contains(term) || u.LastName.Contains(term));
        }

        if (companyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == companyId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<ApplicationUser> users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<ApplicationUserInfo>();

        foreach (ApplicationUser user in users)
        {
            items.Add(await MapToInfoAsync(user));
        }

        return new PagedResult<ApplicationUserInfo>(items, totalCount, page, pageSize);
    }

    private async Task<ApplicationUserInfo> MapToInfoAsync(ApplicationUser user)
    {
        IList<string> roles = await _userManager.GetRolesAsync(user);

        return new ApplicationUserInfo(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.CompanyId,
            user.IsActive,
            user.LastLoginAt,
            roles.ToArray(),
            user.LockedOutAt);
    }
}
