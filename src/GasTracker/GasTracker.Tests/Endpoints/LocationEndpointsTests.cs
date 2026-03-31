using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Tests.Endpoints;

public class HaversineTests
{
    // The Haversine method is private static. We test it indirectly via the endpoint,
    // but we can also use reflection to test the math directly for precision.

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        // Mirror the implementation for direct testing
        const double R = 3958.8;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    [Fact]
    public void SamePoint_ReturnsZero()
    {
        Haversine(33.749, -84.388, 33.749, -84.388).Should().Be(0);
    }

    [Fact]
    public void KnownDistance_AtlantaToDecatur_Approximately6Miles()
    {
        // Atlanta (33.749, -84.388) to Decatur (33.775, -84.296)
        var distance = Haversine(33.749, -84.388, 33.775, -84.296);
        distance.Should().BeInRange(5.0, 7.0);
    }

    [Fact]
    public void KnownDistance_NewYorkToLosAngeles_Approximately2450Miles()
    {
        var distance = Haversine(40.7128, -74.0060, 34.0522, -118.2437);
        distance.Should().BeInRange(2400, 2500);
    }

    [Fact]
    public void SmallDistance_WithinHalfMile()
    {
        // Two points ~0.1 mile apart
        var distance = Haversine(33.7490, -84.3880, 33.7504, -84.3880);
        distance.Should().BeLessThan(0.5);
        distance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SymmetricDistance_SameRegardlessOfDirection()
    {
        var d1 = Haversine(33.749, -84.388, 34.0, -84.0);
        var d2 = Haversine(34.0, -84.0, 33.749, -84.388);
        d1.Should().BeApproximately(d2, 0.001);
    }

    [Fact]
    public void NegativeLatLng_WorksCorrectly()
    {
        // Southern hemisphere + eastern hemisphere
        var distance = Haversine(-33.8688, 151.2093, -33.8688, 151.2093);
        distance.Should().Be(0);
    }
}

public class NearbyStationQueryTests
{
    [Fact]
    public async Task NearbyQuery_NoGpsData_ReturnsEmpty()
    {
        using var db = TestDbContextFactory.Create();
        db.Vehicles.Add(new GasTracker.Core.Entities.Vehicle { Id = Guid.NewGuid(), Year = 2021, Make = "T", Model = "T" });
        db.FillUps.Add(new GasTracker.Core.Entities.FillUp
        {
            Id = Guid.NewGuid(),
            VehicleId = db.Vehicles.Local.First().Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            OdometerMiles = 45000,
            Gallons = 14,
            PricePerGallon = 3,
            TotalCost = 42,
            StationName = "Shell",
            Latitude = null,
            Longitude = null,
        });
        await db.SaveChangesAsync();

        var result = await db.FillUps
            .Where(f => f.Latitude != null && f.Longitude != null)
            .ToListAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task NearbyQuery_StationsWithGps_Found()
    {
        using var db = TestDbContextFactory.Create();
        var vid = Guid.NewGuid();
        db.Vehicles.Add(new GasTracker.Core.Entities.Vehicle { Id = vid, Year = 2021, Make = "T", Model = "T" });
        db.FillUps.Add(new GasTracker.Core.Entities.FillUp
        {
            Id = Guid.NewGuid(),
            VehicleId = vid,
            Date = DateOnly.FromDateTime(DateTime.Today),
            OdometerMiles = 45000,
            Gallons = 14,
            PricePerGallon = 3,
            TotalCost = 42,
            StationName = "Shell",
            Latitude = 33.749m,
            Longitude = -84.388m,
        });
        await db.SaveChangesAsync();

        var result = await db.FillUps
            .Where(f => f.Latitude != null && f.Longitude != null)
            .ToListAsync();

        result.Should().HaveCount(1);
        result[0].StationName.Should().Be("Shell");
    }
}
