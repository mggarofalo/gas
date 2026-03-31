using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GasTracker.Tests.Auth;

public class TokenServiceTests
{
    private static readonly JwtOptions TestJwtOptions = new()
    {
        Key = "TestSigningKeyThatIsAtLeast32BytesLongForHS256!!!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpiryMinutes = 60,
        RefreshTokenExpiryDays = 30,
    };

    private static TokenService CreateService(UserManager<ApplicationUser> userManager)
    {
        return new TokenService(Options.Create(TestJwtOptions), userManager);
    }

    private static UserManager<ApplicationUser> CreateMockUserManager()
    {
        // Use InMemory store via the test DbContext
        var db = TestDbContextFactory.Create();
        var store = new Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<ApplicationUser>(db);
        return new UserManager<ApplicationUser>(
            store, null!, new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), null!,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<UserManager<ApplicationUser>>.Instance);
    }

    [Fact]
    public async Task GenerateTokensAsync_ReturnsValidJwt()
    {
        var userManager = CreateMockUserManager();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM",
            MustResetPassword = false,
        };
        await userManager.CreateAsync(user, "TestPassword123!!");
        var service = CreateService(userManager);

        var (accessToken, refreshToken, expiresIn) = await service.GenerateTokensAsync(user);

        accessToken.Should().NotBeNullOrEmpty();
        refreshToken.Should().NotBeNullOrEmpty();
        expiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task GenerateTokensAsync_JwtContainsCorrectClaims()
    {
        var userManager = CreateMockUserManager();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM",
            MustResetPassword = true,
        };
        await userManager.CreateAsync(user, "TestPassword123!!");
        var service = CreateService(userManager);

        var (accessToken, _, _) = await service.GenerateTokensAsync(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(user.Id);
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("test@example.com");
        jwt.Claims.First(c => c.Type == "must_reset_password").Value.Should().Be("true");
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Jti).Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateTokensAsync_RefreshTokenIs64ByteBase64()
    {
        var userManager = CreateMockUserManager();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM",
        };
        await userManager.CreateAsync(user, "TestPassword123!!");
        var service = CreateService(userManager);

        var (_, refreshToken, _) = await service.GenerateTokensAsync(user);

        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public async Task GenerateTokensAsync_StoresRefreshTokenOnUser()
    {
        var userManager = CreateMockUserManager();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM",
        };
        await userManager.CreateAsync(user, "TestPassword123!!");
        var service = CreateService(userManager);

        var (_, refreshToken, _) = await service.GenerateTokensAsync(user);

        var updated = await userManager.FindByIdAsync(user.Id);
        updated!.RefreshToken.Should().Be(refreshToken);
        updated.RefreshTokenExpiresAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ValidateExpiredToken_AcceptsExpiredButValidToken()
    {
        var userManager = CreateMockUserManager();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM",
        };
        await userManager.CreateAsync(user, "TestPassword123!!");

        // Generate a token that's already expired
        var opts = new JwtOptions
        {
            Key = TestJwtOptions.Key,
            Issuer = TestJwtOptions.Issuer,
            Audience = TestJwtOptions.Audience,
            AccessTokenExpiryMinutes = -1, // Already expired
        };
        var service = new TokenService(Options.Create(opts), userManager);
        var (accessToken, _, _) = await service.GenerateTokensAsync(user);

        var validationService = CreateService(userManager);
        var principal = validationService.ValidateExpiredToken(accessToken);

        principal.Should().NotBeNull();
        var userId = principal!.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        userId.Should().Be(user.Id);
    }

    [Fact]
    public void ValidateExpiredToken_RejectsTamperedToken()
    {
        var service = CreateService(CreateMockUserManager());
        var result = service.ValidateExpiredToken("totally.invalid.token");
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateExpiredToken_RejectsWrongSigningKey()
    {
        // Create a token signed with a different key
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("DifferentKeyThatIsAlsoAtLeast32BytesLong!!!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "user-id")],
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: creds);
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

        var service = CreateService(CreateMockUserManager());
        var result = service.ValidateExpiredToken(tokenStr);
        result.Should().BeNull();
    }
}
