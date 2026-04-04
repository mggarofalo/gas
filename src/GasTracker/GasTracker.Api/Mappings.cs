using GasTracker.Core.DTOs;
using GasTracker.Core.Entities;

namespace GasTracker.Api;

public static class Mappings
{
    public static VehicleDto ToDto(this Vehicle v) => new(
        v.Id, v.Year, v.Make, v.Model, v.Notes, v.OctaneRating, v.IsActive, v.Label, v.CreatedAt, v.UpdatedAt);

    public static FillUpDto ToDto(this FillUp f, int? tripMiles)
    {
        string? receiptUrl = null;
        if (f.ReceiptPath is not null)
            receiptUrl = $"/api/fill-ups/{f.Id}/receipt";

        // Plausibility guard: null out fuel economy fields when data looks bad.
        // tripMiles > 2000 between fill-ups or mpg > 200 indicates bad odometer data.
        // The fill-up is preserved for price/expense history — only computed fields are nulled.
        var plausibleTrip = tripMiles.HasValue && tripMiles > 0 && tripMiles <= 2000;
        decimal? rawMpg = plausibleTrip && f.Gallons > 0
            ? Math.Round((decimal)tripMiles!.Value / f.Gallons, 2)
            : null;
        decimal? mpg = rawMpg is > 0 and <= 200 ? rawMpg : null;
        decimal? costPerMile = plausibleTrip && mpg.HasValue
            ? Math.Round(f.TotalCost / tripMiles!.Value, 2)
            : null;

        return new FillUpDto(
            f.Id, f.VehicleId,
            f.Vehicle?.Label ?? "",
            f.Date.ToString("yyyy-MM-dd"),
            f.OdometerMiles, f.Gallons, f.PricePerGallon, f.TotalCost, f.OctaneRating,
            f.StationName, f.StationAddress,
            f.Latitude, f.Longitude,
            receiptUrl, tripMiles, mpg, costPerMile,
            f.PaperlessSyncStatus, f.PaperlessSyncedAt?.ToString("o"), f.PaperlessSyncError,
            f.YnabSyncStatus, f.YnabAccountId, f.YnabAccountName,
            f.YnabCategoryId, f.YnabCategoryName, f.Notes,
            f.CreatedAt.ToString("o"));
    }
}
