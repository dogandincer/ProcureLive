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

    /// <summary>Kullanıcının bildirimleri e-postayla da alıp almayacağı.</summary>
    Task<bool> SetEmailNotificationsAsync(Guid userId, bool enabled, CancellationToken cancellationToken);

    Task<bool> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken);

    /// <summary>
    /// Hatalı şifre denemesini sayar; deneme sayısı <paramref name="maxFailedAttempts"/>'ı aşarsa hesabı pasife alır ve
    /// true döner. Son aktif Admin hiçbir zaman otomatik pasife alınmaz (sistem yöneticisiz kalmasın).
    /// </summary>
    Task<bool> RegisterFailedLoginAsync(Guid userId, DateTime utcNow, int maxFailedAttempts, CancellationToken cancellationToken);

    Task ResetFailedLoginsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Şifre sıfırlama belirteci (ASP.NET Identity varsayılan sağlayıcısı); kullanıcı yoksa null.</summary>
    Task<string?> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Belirteçle yeni şifre. Hatalı şifre kilidi varsa kaldırılır. Hata kodları: "InvalidToken" veya Türkçe kural mesajları.</summary>
    Task<IdentityOperationResult> ResetPasswordAsync(Guid userId, string token, string newPassword, CancellationToken cancellationToken);

    /// <summary>Mevcut şifreyle değiştirme. Hata kodları: "PasswordMismatch" veya Türkçe kural mesajları.</summary>
    Task<IdentityOperationResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>Rol adına göre Identity rol kimliği (yetki tablosu RolePermissions bu kimliği kullanır).</summary>
    Task<Guid?> GetRoleIdAsync(string roleName, CancellationToken cancellationToken);

    /// <summary><paramref name="roles"/> verilirse yalnızca bu rollerden en az birine sahip kullanıcılar listelenir.</summary>
    Task<PagedResult<ApplicationUserInfo>> ListUsersAsync(
        string? searchTerm,
        Guid? companyId,
        int page,
        int pageSize,
        IReadOnlyCollection<string>? roles,
        CancellationToken cancellationToken);
}
