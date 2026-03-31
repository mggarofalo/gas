using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GasTracker.Infrastructure.Auth;

public class TokenService(IOptions<JwtOptions> opts, UserManager<ApplicationUser> userManager) : ITokenService
{
    private readonly JwtOptions _opts = opts.Value;

    public async Task<(string AccessToken, string RefreshToken, int ExpiresIn)> GenerateTokensAsync(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("must_reset_password", user.MustResetPassword.ToString().ToLowerInvariant()),
        };

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresIn = _opts.AccessTokenExpiryMinutes * 60;

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_opts.AccessTokenExpiryMinutes),
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(_opts.RefreshTokenExpiryDays);
        await userManager.UpdateAsync(user);

        return (accessToken, refreshToken, expiresIn);
    }

    public ClaimsPrincipal? ValidateExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.Key));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _opts.Issuer,
            ValidAudience = _opts.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false, // Allow expired tokens for refresh flow
        };

        try
        {
            return new JwtSecurityTokenHandler().ValidateToken(token, validationParams, out _);
        }
        catch
        {
            return null;
        }
    }
}
