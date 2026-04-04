using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace GasTracker.Tests.Integration;

public class AuthEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AuthEndpointTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var email = $"login-valid-{Guid.NewGuid():N}@test.com";
        const string password = "Test1234!@#$";
        await _factory.SeedUserAsync(email, password);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("expiresIn").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("tokenType").GetString().Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var email = $"login-invalid-{Guid.NewGuid():N}@test.com";
        await _factory.SeedUserAsync(email, "Test1234!@#$");
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "WrongPassword123!@#",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonexistentUser_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "nobody@example.com",
            password = "Password123!@#$",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyBody_ReturnsValidationError()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "",
            password = "",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_ReturnsSuccess()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_WithValidTokens_ReturnsNewTokens()
    {
        var email = $"refresh-valid-{Guid.NewGuid():N}@test.com";
        const string password = "Test1234!@#$";
        await _factory.SeedUserAsync(email, password);
        var client = _factory.CreateClient();

        // First login to get tokens
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString()!;
        var refreshToken = loginBody.GetProperty("refreshToken").GetString()!;

        // Now refresh
        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            accessToken,
            refreshToken,
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        refreshBody.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_WithInvalidRefreshToken_Returns401()
    {
        var email = $"refresh-invalid-{Guid.NewGuid():N}@test.com";
        const string password = "Test1234!@#$";
        await _factory.SeedUserAsync(email, password);
        var client = _factory.CreateClient();

        // Login to get a valid access token
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
        });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString()!;

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            accessToken,
            refreshToken = "InvalidRefreshToken",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithEmptyTokens_ReturnsValidationError()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            accessToken = "",
            refreshToken = "",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsNoContent()
    {
        var email = $"logout-{Guid.NewGuid():N}@test.com";
        const string password = "Test1234!@#$";
        var user = await _factory.SeedUserAsync(email, password);
        var token = _factory.GenerateTestToken(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Login first so the user has a refresh token
        await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_ReturnsNewTokens()
    {
        var email = $"changepw-valid-{Guid.NewGuid():N}@test.com";
        const string password = "Test1234!@#$";
        var user = await _factory.SeedUserAsync(email, password);
        var token = _factory.GenerateTestToken(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = password,
            newPassword = "NewPassword1!@#$",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsValidationError()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "WrongPassword123!@#",
            newPassword = "NewPassword1!@#$",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithEmptyPasswords_ReturnsValidationError()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "",
            newPassword = "",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = TestWebAppFactory.TestUserPassword,
            newPassword = "NewPassword1!@#$",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
