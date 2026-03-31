using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class LocationEndpoints
{
    private const double MaxDistanceMiles = 0.5;
    // Approximate miles per degree at mid-latitudes
    private const double MilesPerDegreeLat = 69.0;
    private const double MilesPerDegreeLng = 54.6; // ~69 * cos(38°)

    public static void MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/locations/nearby", async (decimal lat, decimal lng, AppDbContext db) =>
        {
            var dLat = (decimal)(MaxDistanceMiles / MilesPerDegreeLat);
            var dLng = (decimal)(MaxDistanceMiles / MilesPerDegreeLng);

            // Bounding box pre-filter, then compute actual distance
            var candidates = await db.FillUps
                .Where(f => f.Latitude != null && f.Longitude != null)
                .Where(f => f.Latitude >= lat - dLat && f.Latitude <= lat + dLat)
                .Where(f => f.Longitude >= lng - dLng && f.Longitude <= lng + dLng)
                .Select(f => new
                {
                    f.StationName,
                    f.StationAddress,
                    f.Latitude,
                    f.Longitude,
                })
                .ToListAsync();

            var results = candidates
                .Select(c => new
                {
                    c.StationName,
                    c.StationAddress,
                    DistanceMiles = HaversineDistanceMiles(
                        (double)lat, (double)lng,
                        (double)c.Latitude!, (double)c.Longitude!),
                })
                .Where(c => c.DistanceMiles <= MaxDistanceMiles)
                .GroupBy(c => (c.StationName, c.StationAddress))
                .Select(g => new
                {
                    g.Key.StationName,
                    g.Key.StationAddress,
                    DistanceMiles = Math.Round(g.Min(x => x.DistanceMiles), 3),
                    VisitCount = g.Count(),
                })
                .OrderBy(c => c.DistanceMiles)
                .ToList();

            return Results.Ok(results);
        }).WithTags("Locations").RequireAuthorization();

        app.MapGet("/api/stations/search", async (string q, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Results.Ok(Array.Empty<object>());

            var results = await db.FillUps
                .Where(f => f.StationName.ToLower().Contains(q.ToLower()))
                .GroupBy(f => f.StationName)
                .Select(g => new
                {
                    StationName = g.Key,
                    VisitCount = g.Count(),
                    LastVisit = g.Max(f => f.Date),
                })
                .OrderByDescending(s => s.VisitCount)
                .Take(5)
                .ToListAsync();

            return Results.Ok(results);
        }).WithTags("Locations").RequireAuthorization();
    }

    private static double HaversineDistanceMiles(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 3958.8; // Earth radius in miles
        var dLat = ToRad(lat2 - lat1);
        var dLng = ToRad(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
