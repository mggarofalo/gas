using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Ynab;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api.Endpoints;

public static class YnabImportEndpoints
{
    public static void MapYnabImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ynab/imports").WithTags("YnabImports").RequireAuthorization();

        // Trigger pull sync
        group.MapPost("/pull", async (PullRequest? req, YnabPullSyncService pullSync, YnabTokenService tokenService, AppDbContext db) =>
        {
            var settings = await db.YnabSettings.FirstOrDefaultAsync();
            if (settings is null || string.IsNullOrWhiteSpace(settings.PlanId) || string.IsNullOrWhiteSpace(settings.AccountId))
                return Results.BadRequest(new { error = "YNAB is not configured with a plan and account" });

            try
            {
                var token = await tokenService.GetDecryptedTokenAsync();
                var sinceDate = req?.SinceDate is not null ? DateOnly.Parse(req.SinceDate) : (DateOnly?)null;
                var result = await pullSync.PullAsync(token, settings.PlanId, settings.AccountId, sinceDate, settings.LastServerKnowledge);
                return Results.Ok(new
                {
                    newImports = result.NewImports,
                    skipped = result.Skipped,
                    errors = result.Errors,
                    errorMessages = result.ErrorMessages,
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // List pending imports
        group.MapGet("/", async (string? status, int? page, int? pageSize, AppDbContext db) =>
        {
            var filterStatus = status ?? "pending";
            var p = page ?? 1;
            var ps = Math.Clamp(pageSize ?? 50, 1, 200);

            var query = db.YnabImports.Where(i => i.Status == filterStatus);
            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(i => i.Date)
                .Skip((p - 1) * ps)
                .Take(ps)
                .Select(i => new YnabImportDto(
                    i.Id, i.YnabTransactionId, i.Date.ToString("yyyy-MM-dd"),
                    i.PayeeName, i.AmountMilliunits, i.Memo,
                    i.Gallons, i.PricePerGallon, i.OctaneRating, i.OdometerMiles,
                    i.VehicleName, i.VehicleId, i.Status))
                .ToListAsync();

            return Results.Ok(new { items, page = p, pageSize = ps, totalCount });
        });

        // Update parsed fields
        group.MapPut("/{id:guid}", async (Guid id, UpdateImportRequest req, AppDbContext db) =>
        {
            var import = await db.YnabImports.FindAsync(id);
            if (import is null) return Results.NotFound();
            if (import.Status != "pending") return Results.BadRequest(new { error = "Import is not pending" });

            if (req.Gallons.HasValue) import.Gallons = req.Gallons;
            if (req.PricePerGallon.HasValue) import.PricePerGallon = req.PricePerGallon;
            if (req.OctaneRating.HasValue) import.OctaneRating = req.OctaneRating;
            if (req.OdometerMiles.HasValue) import.OdometerMiles = req.OdometerMiles;
            if (req.VehicleId.HasValue) import.VehicleId = req.VehicleId;

            await db.SaveChangesAsync();
            return Results.Ok(ToDto(import));
        });

        // Approve single import → create fill-up
        group.MapPost("/{id:guid}/approve", async (Guid id, AppDbContext db, IVehicleRepository vehicleRepo) =>
        {
            var import = await db.YnabImports.FindAsync(id);
            if (import is null) return Results.NotFound();
            if (import.Status != "pending") return Results.BadRequest(new { error = "Import is not pending" });

            var validationError = ValidateForApproval(import);
            if (validationError is not null) return Results.BadRequest(new { error = validationError });

            var vehicle = await vehicleRepo.GetByIdAsync(import.VehicleId!.Value);
            if (vehicle is null) return Results.BadRequest(new { error = "Vehicle not found" });

            var fillUp = CreateFillUpFromImport(import);
            db.FillUps.Add(fillUp);
            import.Status = "approved";
            await db.SaveChangesAsync();

            return Results.Ok(new { fillUpId = fillUp.Id });
        });

        // Bulk approve all complete imports
        group.MapPost("/approve-all", async (AppDbContext db, IVehicleRepository vehicleRepo) =>
        {
            var pending = await db.YnabImports
                .Where(i => i.Status == "pending"
                    && i.VehicleId != null
                    && i.Gallons != null && i.Gallons > 0
                    && i.PricePerGallon != null && i.PricePerGallon > 0
                    && i.OdometerMiles != null && i.OdometerMiles > 0)
                .ToListAsync();

            var approved = 0;
            foreach (var import in pending)
            {
                var vehicle = await vehicleRepo.GetByIdAsync(import.VehicleId!.Value);
                if (vehicle is null) continue;

                db.FillUps.Add(CreateFillUpFromImport(import));
                import.Status = "approved";
                approved++;
            }

            if (approved > 0)
                await db.SaveChangesAsync();

            return Results.Ok(new { approved });
        });

        // Dismiss import
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var import = await db.YnabImports.FindAsync(id);
            if (import is null) return Results.NotFound();
            import.Status = "dismissed";
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static string? ValidateForApproval(YnabImport import)
    {
        if (!import.VehicleId.HasValue) return "Vehicle is required";
        if (!import.Gallons.HasValue || import.Gallons <= 0) return "Gallons must be > 0";
        if (!import.PricePerGallon.HasValue || import.PricePerGallon <= 0) return "Price per gallon must be > 0";
        if (!import.OdometerMiles.HasValue || import.OdometerMiles <= 0) return "Odometer miles must be > 0";
        return null;
    }

    private static FillUp CreateFillUpFromImport(YnabImport import)
    {
        var totalCost = Math.Abs(import.AmountMilliunits) / 1000m;
        return new FillUp
        {
            VehicleId = import.VehicleId!.Value,
            Date = import.Date,
            OdometerMiles = import.OdometerMiles!.Value,
            Gallons = import.Gallons!.Value,
            PricePerGallon = import.PricePerGallon!.Value,
            TotalCost = totalCost,
            OctaneRating = import.OctaneRating,
            StationName = import.PayeeName,
            YnabTransactionId = import.YnabTransactionId,
            YnabSyncStatus = "synced",
        };
    }

    private static YnabImportDto ToDto(YnabImport i) => new(
        i.Id, i.YnabTransactionId, i.Date.ToString("yyyy-MM-dd"),
        i.PayeeName, i.AmountMilliunits, i.Memo,
        i.Gallons, i.PricePerGallon, i.OctaneRating, i.OdometerMiles,
        i.VehicleName, i.VehicleId, i.Status);

    private record PullRequest(string? SinceDate);

    private record UpdateImportRequest(
        decimal? Gallons,
        decimal? PricePerGallon,
        short? OctaneRating,
        int? OdometerMiles,
        Guid? VehicleId);

    private record YnabImportDto(
        Guid Id, string YnabTransactionId, string Date,
        string PayeeName, long AmountMilliunits, string? Memo,
        decimal? Gallons, decimal? PricePerGallon, short? OctaneRating, int? OdometerMiles,
        string? VehicleName, Guid? VehicleId, string Status);
}
