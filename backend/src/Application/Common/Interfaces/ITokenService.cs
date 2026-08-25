using ProcureLive.Application.Common.Models;

namespace ProcureLive.Application.Common.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(ApplicationUserInfo user);

    string GenerateRefreshToken();
}
