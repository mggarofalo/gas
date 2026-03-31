using GasTracker.Core.Interfaces;

namespace GasTracker.Api.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats", async (IFillUpRepository repo, Guid? vehicleId, DateOnly? startDate, DateOnly? endDate) =>
        {
            var stats = await repo.GetStatsAsync(vehicleId, startDate, endDate);
            return Results.Ok(stats);
        }).WithTags("Stats").RequireAuthorization();
    }
}
