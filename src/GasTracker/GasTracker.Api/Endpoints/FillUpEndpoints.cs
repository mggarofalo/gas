using FluentValidation;
using GasTracker.Core.DTOs;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;

namespace GasTracker.Api.Endpoints;

public static class FillUpEndpoints
{
    private static readonly HashSet<string> AllowedContentTypes =
        ["image/jpeg", "image/png", "image/webp", "application/pdf"];
    private const long MaxReceiptSize = 10 * 1024 * 1024;

    public static void MapFillUpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fill-ups").WithTags("FillUps");

        group.MapGet("/", async (
            IFillUpRepository repo, IReceiptStore receiptStore,
            Guid? vehicleId, DateOnly? startDate, DateOnly? endDate,
            int? page, int? pageSize, string? sortBy, string? sortDir) =>
        {
            var p = page ?? 1;
            var ps = Math.Clamp(pageSize ?? 25, 1, 100);
            var (items, totalCount) = await repo.ListAsync(vehicleId, startDate, endDate, p, ps, sortBy ?? "date", sortDir ?? "desc");

            var dtos = new List<FillUpDto>();
            foreach (var f in items)
            {
                var tripMiles = await repo.GetTripMilesAsync(f);
                dtos.Add(ToDto(f, tripMiles, receiptStore));
            }

            return Results.Ok(new FillUpPageDto(dtos, p, ps, totalCount));
        });

        group.MapGet("/{id:guid}", async (Guid id, IFillUpRepository repo, IReceiptStore receiptStore) =>
        {
            var fillUp = await repo.GetByIdAsync(id);
            if (fillUp is null) return Results.NotFound();
            var tripMiles = await repo.GetTripMilesAsync(fillUp);
            return Results.Ok(ToDto(fillUp, tripMiles, receiptStore));
        });

        group.MapPost("/", async (HttpRequest request, IFillUpRepository repo, IVehicleRepository vehicleRepo, IReceiptStore receiptStore, IValidator<CreateFillUpRequest> validator) =>
        {
            var form = await request.ReadFormAsync();

            var req = new CreateFillUpRequest(
                VehicleId: Guid.Parse(form["vehicleId"].ToString()),
                Date: form["date"].ToString(),
                OdometerMiles: int.Parse(form["odometerMiles"].ToString()),
                Gallons: decimal.Parse(form["gallons"].ToString()),
                PricePerGallon: decimal.Parse(form["pricePerGallon"].ToString()),
                TotalCost: form.ContainsKey("totalCost") && !string.IsNullOrEmpty(form["totalCost"].ToString()) ? decimal.Parse(form["totalCost"].ToString()) : null,
                StationName: form["stationName"].ToString(),
                StationAddress: form.TryGetValue("stationAddress", out var addr) ? addr.ToString() : null,
                Latitude: form.TryGetValue("latitude", out var lat) && !string.IsNullOrEmpty(lat.ToString()) ? decimal.Parse(lat.ToString()) : null,
                Longitude: form.TryGetValue("longitude", out var lng) && !string.IsNullOrEmpty(lng.ToString()) ? decimal.Parse(lng.ToString()) : null,
                Notes: form.TryGetValue("notes", out var notes) ? notes.ToString() : null);

            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var vehicle = await vehicleRepo.GetByIdAsync(req.VehicleId);
            if (vehicle is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["vehicleId"] = ["Vehicle not found"] });

            var fillUp = new FillUp
            {
                VehicleId = req.VehicleId,
                Date = DateOnly.Parse(req.Date),
                OdometerMiles = req.OdometerMiles,
                Gallons = req.Gallons,
                PricePerGallon = req.PricePerGallon,
                TotalCost = req.TotalCost ?? Math.Round(req.Gallons * req.PricePerGallon, 2),
                StationName = req.StationName,
                StationAddress = req.StationAddress,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                Notes = req.Notes,
            };

            await repo.CreateAsync(fillUp);
            fillUp.Vehicle = vehicle;

            // Handle receipt upload
            var receipt = form.Files.GetFile("receipt");
            if (receipt is not null)
            {
                var receiptError = ValidateReceipt(receipt);
                if (receiptError is not null) return receiptError;

                using var stream = receipt.OpenReadStream();
                fillUp.ReceiptPath = await receiptStore.UploadAsync(
                    fillUp.VehicleId, fillUp.Id, receipt.FileName, receipt.ContentType, stream);
                fillUp.PaperlessSyncStatus = "pending";
                await repo.UpdateAsync(fillUp);
            }

            var tripMiles = await repo.GetTripMilesAsync(fillUp);
            return Results.Created($"/api/fill-ups/{fillUp.Id}", ToDto(fillUp, tripMiles, receiptStore));
        }).DisableAntiforgery();

        group.MapPut("/{id:guid}", async (Guid id, HttpRequest request, IFillUpRepository repo, IReceiptStore receiptStore) =>
        {
            var fillUp = await repo.GetByIdAsync(id);
            if (fillUp is null) return Results.NotFound();

            var form = await request.ReadFormAsync();

            if (form.ContainsKey("date")) fillUp.Date = DateOnly.Parse(form["date"].ToString());
            if (form.ContainsKey("odometerMiles")) fillUp.OdometerMiles = int.Parse(form["odometerMiles"].ToString());
            if (form.ContainsKey("gallons")) fillUp.Gallons = decimal.Parse(form["gallons"].ToString());
            if (form.ContainsKey("pricePerGallon")) fillUp.PricePerGallon = decimal.Parse(form["pricePerGallon"].ToString());
            if (form.ContainsKey("totalCost")) fillUp.TotalCost = decimal.Parse(form["totalCost"].ToString());
            if (form.ContainsKey("stationName")) fillUp.StationName = form["stationName"].ToString();
            if (form.ContainsKey("stationAddress")) fillUp.StationAddress = form["stationAddress"].ToString();
            if (form.ContainsKey("latitude") && !string.IsNullOrEmpty(form["latitude"].ToString())) fillUp.Latitude = decimal.Parse(form["latitude"].ToString());
            if (form.ContainsKey("longitude") && !string.IsNullOrEmpty(form["longitude"].ToString())) fillUp.Longitude = decimal.Parse(form["longitude"].ToString());
            if (form.ContainsKey("notes")) fillUp.Notes = form["notes"].ToString();

            var receipt = form.Files.GetFile("receipt");
            if (receipt is not null)
            {
                var receiptError = ValidateReceipt(receipt);
                if (receiptError is not null) return receiptError;

                if (fillUp.ReceiptPath is not null)
                    await receiptStore.DeleteAsync(fillUp.ReceiptPath);

                using var stream = receipt.OpenReadStream();
                fillUp.ReceiptPath = await receiptStore.UploadAsync(
                    fillUp.VehicleId, fillUp.Id, receipt.FileName, receipt.ContentType, stream);
                fillUp.PaperlessSyncStatus = "pending";
                fillUp.PaperlessSyncAttempts = 0;
            }

            await repo.UpdateAsync(fillUp);
            var tripMiles = await repo.GetTripMilesAsync(fillUp);
            return Results.Ok(ToDto(fillUp, tripMiles, receiptStore));
        }).DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, IFillUpRepository repo, IReceiptStore receiptStore) =>
        {
            var fillUp = await repo.GetByIdAsync(id);
            if (fillUp is null) return Results.NotFound();

            if (fillUp.ReceiptPath is not null)
                await receiptStore.DeleteAsync(fillUp.ReceiptPath);

            await repo.DeleteAsync(fillUp);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}/receipt", async (Guid id, IFillUpRepository repo, IReceiptStore receiptStore) =>
        {
            var fillUp = await repo.GetByIdAsync(id);
            if (fillUp?.ReceiptPath is null) return Results.NotFound();
            var url = await receiptStore.GetPresignedUrlAsync(fillUp.ReceiptPath, TimeSpan.FromHours(1));
            return Results.Redirect(url);
        });

        group.MapPost("/{id:guid}/resync", async (Guid id, IFillUpRepository repo) =>
        {
            var fillUp = await repo.GetByIdAsync(id);
            if (fillUp is null) return Results.NotFound();
            if (fillUp.ReceiptPath is null)
                return Results.BadRequest(new { error = "No receipt attached" });
            fillUp.PaperlessSyncStatus = "pending";
            fillUp.PaperlessSyncAttempts = 0;
            fillUp.PaperlessSyncError = null;
            await repo.UpdateAsync(fillUp);
            return Results.NoContent();
        });
    }

    private static IResult? ValidateReceipt(IFormFile receipt)
    {
        if (receipt.Length > MaxReceiptSize)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = ["File exceeds 10 MB limit"] });
        if (!AllowedContentTypes.Contains(receipt.ContentType))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = [$"File type {receipt.ContentType} not allowed"] });
        return null;
    }

    private static FillUpDto ToDto(FillUp f, int? tripMiles, IReceiptStore receiptStore)
    {
        string? receiptUrl = null;
        if (f.ReceiptPath is not null)
            receiptUrl = $"/api/fill-ups/{f.Id}/receipt";

        decimal? mpg = tripMiles.HasValue && tripMiles > 0 && f.Gallons > 0
            ? Math.Round((decimal)tripMiles.Value / f.Gallons, 2)
            : null;
        decimal? costPerMile = tripMiles.HasValue && tripMiles > 0
            ? Math.Round(f.TotalCost / tripMiles.Value, 2)
            : null;

        return new FillUpDto(
            f.Id, f.VehicleId,
            f.Vehicle?.Label ?? "",
            f.Date.ToString("yyyy-MM-dd"),
            f.OdometerMiles, f.Gallons, f.PricePerGallon, f.TotalCost,
            f.StationName, f.StationAddress,
            f.Latitude, f.Longitude,
            receiptUrl, tripMiles, mpg, costPerMile,
            f.PaperlessSyncStatus, f.Notes,
            f.CreatedAt.ToString("o"));
    }
}
