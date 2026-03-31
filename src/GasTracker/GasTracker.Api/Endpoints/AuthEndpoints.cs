using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GasTracker.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest req, UserManager<ApplicationUser> userManager, ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [""] = ["Email and password are required"] });

            var user = await userManager.FindByEmailAsync(req.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, req.Password))
                return Results.Problem(detail: "Invalid credentials", statusCode: 401);

            user.LastLoginAt = DateTimeOffset.UtcNow;
            await userManager.UpdateAsync(user);

            var (accessToken, refreshToken, expiresIn) = await tokenService.GenerateTokensAsync(user);
            return Results.Ok(new TokenResponse(accessToken, refreshToken, expiresIn, user.MustResetPassword, "Bearer"));
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest req, UserManager<ApplicationUser> userManager, ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(req.AccessToken) || string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [""] = ["Both tokens are required"] });

            var principal = tokenService.ValidateExpiredToken(req.AccessToken);
            if (principal is null)
                return Results.Problem(detail: "Invalid access token", statusCode: 401);

            var userId = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null)
                return Results.Problem(detail: "Invalid token claims", statusCode: 401);

            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.RefreshToken != req.RefreshToken || user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow)
                return Results.Problem(detail: "Invalid or expired refresh token", statusCode: 401);

            var (accessToken, refreshToken, expiresIn) = await tokenService.GenerateTokensAsync(user);
            return Results.Ok(new TokenResponse(accessToken, refreshToken, expiresIn, user.MustResetPassword, "Bearer"));
        }).AllowAnonymous();

        group.MapPost("/logout", async (ClaimsPrincipal claims, UserManager<ApplicationUser> userManager) =>
        {
            var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? claims.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null) return Results.Unauthorized();

            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await userManager.UpdateAsync(user);
            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordRequest req, ClaimsPrincipal claims, UserManager<ApplicationUser> userManager, ITokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Results.ValidationProblem(new Dictionary<string, string[]> { [""] = ["Both passwords are required"] });

            var userId = claims.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? claims.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null) return Results.Unauthorized();

            var result = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
            if (!result.Succeeded)
                return Results.ValidationProblem(result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description }));

            user.MustResetPassword = false;
            await userManager.UpdateAsync(user);

            var (accessToken, refreshToken, expiresIn) = await tokenService.GenerateTokensAsync(user);
            return Results.Ok(new TokenResponse(accessToken, refreshToken, expiresIn, false, "Bearer"));
        }).RequireAuthorization();
    }

    private record LoginRequest(string Email, string Password);
    private record RefreshRequest(string AccessToken, string RefreshToken);
    private record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    private record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn, bool MustResetPassword, string TokenType);
}
