using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;

namespace GasTracker.Tests.Integration;

public class HealthEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public HealthEndpointTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_WithoutVersionConfig_ReportsDevAndNoCommit()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("version").GetString().Should().Be("dev");
        body.GetProperty("commit").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Health_WithVersionConfig_ReportsVersionAndCommit()
    {
        using var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("APP_VERSION", "9.9.9");
            builder.UseSetting("APP_COMMIT", "abc1234def");
        });
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("version").GetString().Should().Be("9.9.9");
        body.GetProperty("commit").GetString().Should().Be("abc1234def");
    }

    [Fact]
    public async Task Health_IsAnonymous()
    {
        // No Authorization header — version visibility must not require login
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
