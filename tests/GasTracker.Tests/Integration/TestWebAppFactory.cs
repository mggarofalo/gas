using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Auth;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Paperless;
using GasTracker.Infrastructure.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace GasTracker.Tests.Integration;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "ThisIsATestSigningKeyThatIsLongEnoughForHS256!";
    public const string TestJwtIssuer = "gas-api";
    public const string TestJwtAudience = "gas-app";
    public const string TestUserEmail = "test@example.com";
    public const string TestUserPassword = "Test1234!@#$";

    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Set configuration values that Program.cs reads before services are configured.
        // These must be set via UseSetting so they're available during top-level statement execution.
        var tempDpKeysDir = Path.Combine(Path.GetTempPath(), "gas-tests-dp-keys", _dbName);
        builder.UseSetting("DataProtection:KeysPath", tempDpKeysDir);
        builder.UseSetting("Jwt:Key", TestJwtKey);
        builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
        builder.UseSetting("Jwt:Audience", TestJwtAudience);

        builder.ConfigureServices(services =>
        {
            // Remove ALL DbContext-related registrations so only InMemory remains.
            // This prevents the "multiple database providers" error.
            // We must remove DbContextOptions<AppDbContext> (which contains the Npgsql config)
            // and the AppDbContext registration itself, then re-add both with InMemory.
            var typesToRemove = new[]
            {
                typeof(DbContextOptions<AppDbContext>),
                typeof(DbContextOptions),
                typeof(AppDbContext),
            };
            foreach (var serviceType in typesToRemove)
            {
                var descriptors = services.Where(d => d.ServiceType == serviceType).ToList();
                foreach (var d in descriptors)
                    services.Remove(d);
            }

            // Build fresh InMemory options without any leftover Npgsql extensions
            services.AddSingleton<DbContextOptions<AppDbContext>>(_ =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                optionsBuilder.UseInMemoryDatabase(_dbName);
                return optionsBuilder.Options;
            });
            services.AddScoped<AppDbContext>();

            // Replace IReceiptStore with a mock
            services.RemoveAll<IReceiptStore>();
            var mockReceiptStore = new Mock<IReceiptStore>();
            mockReceiptStore.Setup(r => r.EnsureBucketExistsAsync()).Returns(Task.CompletedTask);
            mockReceiptStore.Setup(r => r.UploadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>()))
                .ReturnsAsync("test/receipt/path.jpg");
            mockReceiptStore.Setup(r => r.DownloadAsync(It.IsAny<string>()))
                .ReturnsAsync(new MemoryStream());
            mockReceiptStore.Setup(r => r.DeleteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockReceiptStore.Setup(r => r.GetPresignedUrlAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync("https://example.com/receipt");
            services.AddSingleton(mockReceiptStore.Object);

            // Replace IYnabClient with a mock
            services.RemoveAll<IYnabClient>();
            var mockYnabClient = new Mock<IYnabClient>();
            services.AddSingleton(mockYnabClient.Object);

            // Replace IPaperlessClient with a mock
            services.RemoveAll<IPaperlessClient>();
            var mockPaperlessClient = new Mock<IPaperlessClient>();
            mockPaperlessClient.Setup(p => p.IsHealthyAsync()).ReturnsAsync(true);
            services.AddSingleton(mockPaperlessClient.Object);

            // Remove PaperlessSyncService hosted service (accesses external systems)
            var paperlessSyncDescriptor = services
                .Where(d => d.ImplementationType == typeof(PaperlessSyncService))
                .ToList();
            foreach (var d in paperlessSyncDescriptor)
                services.Remove(d);

            // Remove MinioHealthCheck (requires real MinIO connection)
            var healthCheckDescriptors = services
                .Where(d => d.ServiceType == typeof(MinioHealthCheck)
                         || (d.ImplementationType == typeof(MinioHealthCheck)))
                .ToList();
            foreach (var d in healthCheckDescriptors)
                services.Remove(d);

            // Configure JWT with test keys
            services.Configure<JwtOptions>(opts =>
            {
                opts.Key = TestJwtKey;
                opts.Issuer = TestJwtIssuer;
                opts.Audience = TestJwtAudience;
                opts.AccessTokenExpiryMinutes = 60;
                opts.RefreshTokenExpiryDays = 30;
            });

            // Override MinIO options so the health check factory doesn't blow up
            services.Configure<MinioOptions>(opts =>
            {
                opts.Endpoint = "localhost:9000";
                opts.AccessKey = "test";
                opts.SecretKey = "test";
                opts.BucketName = "test";
                opts.UseSSL = false;
            });
        });

        // JWT is configured via UseSetting above, so Program.cs reads the test key.
        // No need to re-register the Bearer scheme — that would cause a duplicate error.
    }

    /// <summary>
    /// Seeds a test user with known credentials and returns the user.
    /// </summary>
    public async Task<ApplicationUser> SeedTestUserAsync()
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByEmailAsync(TestUserEmail);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            UserName = TestUserEmail,
            Email = TestUserEmail,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            MustResetPassword = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, TestUserPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    /// <summary>
    /// Seeds a unique user with the given email and password. Use for tests that modify user state
    /// (e.g., change password) to avoid polluting the shared test user.
    /// </summary>
    public async Task<ApplicationUser> SeedUserAsync(string email, string password)
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = "Test",
            LastName = "User",
            MustResetPassword = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to seed user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    /// <summary>
    /// Generates a valid JWT access token for the given user.
    /// </summary>
    public string GenerateTestToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("must_reset_password", user.MustResetPassword.ToString().ToLowerInvariant()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an HttpClient with a valid Authorization header for the test user.
    /// </summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var user = await SeedTestUserAsync();
        var token = GenerateTestToken(user);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
