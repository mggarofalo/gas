using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace GasTracker.Tests.Integration;

public class VehicleEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public VehicleEndpointTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateVehicle_WithValidData_Returns201WithLocationHeader()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2024,
            make = "Toyota",
            model = "Camry",
            notes = "Test vehicle",
            octaneRating = (short)87,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith("/api/vehicles/");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("year").GetInt16().Should().Be(2024);
        body.GetProperty("make").GetString().Should().Be("Toyota");
        body.GetProperty("model").GetString().Should().Be("Camry");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetVehicleById_AfterCreate_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a vehicle first
        var createResponse = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2023,
            make = "Honda",
            model = "Civic",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        // Get by ID
        var response = await client.GetAsync($"/api/vehicles/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetString().Should().Be(id);
        body.GetProperty("make").GetString().Should().Be("Honda");
        body.GetProperty("model").GetString().Should().Be("Civic");
    }

    [Fact]
    public async Task GetVehicleById_NonexistentId_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/vehicles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListVehicles_ReturnsOkWithArray()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a vehicle to ensure at least one exists
        await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2022,
            make = "Ford",
            model = "F-150",
        });

        var response = await client.GetAsync("/api/vehicles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.ValueKind.Should().Be(JsonValueKind.Array);
        body.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task UpdateVehicle_WithValidData_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a vehicle
        var createResponse = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2021,
            make = "Chevy",
            model = "Malibu",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        // Update it
        var response = await client.PutAsJsonAsync($"/api/vehicles/{id}", new
        {
            make = "Chevrolet",
            notes = "Updated notes",
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("make").GetString().Should().Be("Chevrolet");
        body.GetProperty("notes").GetString().Should().Be("Updated notes");
        // Year should remain unchanged
        body.GetProperty("year").GetInt16().Should().Be(2021);
    }

    [Fact]
    public async Task UpdateVehicle_NonexistentId_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/vehicles/{Guid.NewGuid()}", new
        {
            make = "Test",
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVehicle_SoftDeletes_Returns204()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Create a vehicle
        var createResponse = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2020,
            make = "Nissan",
            model = "Altima",
        });
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();

        // Delete it
        var deleteResponse = await client.DeleteAsync($"/api/vehicles/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's not in the active list
        var listResponse = await client.GetAsync("/api/vehicles?active=true");
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var activeIds = list.EnumerateArray()
            .Select(v => v.GetProperty("id").GetString())
            .ToList();
        activeIds.Should().NotContain(id);

        // But it exists when requesting all (active=false means list all including inactive)
        var allResponse = await client.GetAsync("/api/vehicles?active=false");
        var allList = await allResponse.Content.ReadFromJsonAsync<JsonElement>();
        var allIds = allList.EnumerateArray()
            .Select(v => v.GetProperty("id").GetString())
            .ToList();
        allIds.Should().Contain(id);
    }

    [Fact]
    public async Task DeleteVehicle_NonexistentId_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.DeleteAsync($"/api/vehicles/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVehicle_WithInvalidYear_ReturnsValidationError()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)1800,
            make = "Test",
            model = "Car",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateVehicle_WithEmptyMake_ReturnsValidationError()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2024,
            make = "",
            model = "Test",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateVehicle_WithInvalidOctane_ReturnsValidationError()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2024,
            make = "Test",
            model = "Car",
            octaneRating = (short)95,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AllVehicleEndpoints_WithoutAuth_Return401()
    {
        var client = _factory.CreateClient();

        var getAll = await client.GetAsync("/api/vehicles");
        getAll.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var getOne = await client.GetAsync($"/api/vehicles/{Guid.NewGuid()}");
        getOne.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var create = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2024,
            make = "Test",
            model = "Car",
        });
        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var update = await client.PutAsJsonAsync($"/api/vehicles/{Guid.NewGuid()}", new
        {
            make = "Test",
        });
        update.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var delete = await client.DeleteAsync($"/api/vehicles/{Guid.NewGuid()}");
        delete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
