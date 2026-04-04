using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GasTracker.Tests.Integration;

public class StatsEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public StatsEndpointTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStats_EmptyDatabase_ReturnsAllZeros()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        // Use a nonexistent vehicleId to guarantee zero results regardless of other test data
        var response = await client.GetAsync($"/api/stats?vehicleId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalFillUps").GetInt32().Should().Be(0);
        body.GetProperty("totalGallons").GetDecimal().Should().Be(0m);
        body.GetProperty("totalCost").GetDecimal().Should().Be(0m);
        body.GetProperty("totalMiles").GetInt32().Should().Be(0);
        body.GetProperty("avgMpg").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("avgPricePerGallon").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("avgCostPerFillUp").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("costPerMile").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetStats_WithFillUps_ReturnsCorrectAggregations()
    {
        // Seed fill-ups directly in the database
        var vehicleId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var vehicle = new Vehicle
            {
                Id = vehicleId,
                Year = 2024,
                Make = "Toyota",
                Model = "StatsTest",
                IsActive = true,
            };
            db.Vehicles.Add(vehicle);

            db.FillUps.Add(new FillUp
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                Date = new DateOnly(2024, 1, 1),
                OdometerMiles = 10000,
                Gallons = 10.0m,
                PricePerGallon = 3.499m,
                TotalCost = 34.99m,
                StationName = "Shell",
            });
            db.FillUps.Add(new FillUp
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                Date = new DateOnly(2024, 1, 15),
                OdometerMiles = 10300,
                Gallons = 12.0m,
                PricePerGallon = 3.299m,
                TotalCost = 39.59m,
                StationName = "BP",
            });
            db.FillUps.Add(new FillUp
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId,
                Date = new DateOnly(2024, 2, 1),
                OdometerMiles = 10600,
                Gallons = 11.0m,
                PricePerGallon = 3.199m,
                TotalCost = 35.19m,
                StationName = "Exxon",
            });

            await db.SaveChangesAsync();
        }

        var client = await _factory.CreateAuthenticatedClientAsync();

        // Filter by vehicleId to avoid pollution from other tests
        var response = await client.GetAsync($"/api/stats?vehicleId={vehicleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalFillUps").GetInt32().Should().Be(3);
        body.GetProperty("totalGallons").GetDecimal().Should().Be(33.0m);

        var totalCost = body.GetProperty("totalCost").GetDecimal();
        totalCost.Should().BeApproximately(109.77m, 0.01m);

        // Total miles = max(10600) - min(10000) = 600
        body.GetProperty("totalMiles").GetInt32().Should().Be(600);

        // avgMpg = 600 / 33.0 = ~18.2
        var avgMpg = body.GetProperty("avgMpg").GetDecimal();
        avgMpg.Should().BeApproximately(18.2m, 0.1m);

        // avgPricePerGallon = avg(3.499, 3.299, 3.199) = ~3.332
        var avgPrice = body.GetProperty("avgPricePerGallon").GetDecimal();
        avgPrice.Should().BeApproximately(3.332m, 0.01m);

        // avgCostPerFillUp = 109.77 / 3 = ~36.59
        var avgCost = body.GetProperty("avgCostPerFillUp").GetDecimal();
        avgCost.Should().BeApproximately(36.59m, 0.01m);

        // costPerMile = 109.77 / 600 = ~0.18
        var costPerMile = body.GetProperty("costPerMile").GetDecimal();
        costPerMile.Should().BeApproximately(0.18m, 0.01m);
    }

    [Fact]
    public async Task GetStats_FilterByVehicle_ReturnsOnlyThatVehicleStats()
    {
        var vehicleId1 = Guid.NewGuid();
        var vehicleId2 = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Vehicles.Add(new Vehicle { Id = vehicleId1, Year = 2024, Make = "Honda", Model = "Accord", IsActive = true });
            db.Vehicles.Add(new Vehicle { Id = vehicleId2, Year = 2023, Make = "Ford", Model = "F150", IsActive = true });

            db.FillUps.Add(new FillUp
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId1,
                Date = new DateOnly(2024, 3, 1),
                OdometerMiles = 5000,
                Gallons = 8.0m,
                PricePerGallon = 3.50m,
                TotalCost = 28.00m,
                StationName = "Shell",
            });
            db.FillUps.Add(new FillUp
            {
                Id = Guid.NewGuid(),
                VehicleId = vehicleId2,
                Date = new DateOnly(2024, 3, 1),
                OdometerMiles = 20000,
                Gallons = 15.0m,
                PricePerGallon = 3.80m,
                TotalCost = 57.00m,
                StationName = "Citgo",
            });

            await db.SaveChangesAsync();
        }

        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/stats?vehicleId={vehicleId1}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalFillUps").GetInt32().Should().Be(1);
        body.GetProperty("totalGallons").GetDecimal().Should().Be(8.0m);
        body.GetProperty("totalCost").GetDecimal().Should().Be(28.00m);
    }

    [Fact]
    public async Task GetStats_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stats");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
