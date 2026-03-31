using System.Security.Claims;
using GasTracker.Core.Entities;

namespace GasTracker.Core.Interfaces;

public interface ITokenService
{
    Task<(string AccessToken, string RefreshToken, int ExpiresIn)> GenerateTokensAsync(ApplicationUser user);
    ClaimsPrincipal? ValidateExpiredToken(string token);
}
