namespace GasTracker.Core.DTOs;

public record FillUpDto(
    Guid Id,
    Guid VehicleId,
    string VehicleLabel,
    string Date,
    int OdometerMiles,
    decimal Gallons,
    decimal PricePerGallon,
    decimal TotalCost,
    short? OctaneRating,
    string StationName,
    string? StationAddress,
    decimal? Latitude,
    decimal? Longitude,
    string? ReceiptUrl,
    int? TripMiles,
    decimal? Mpg,
    decimal? CostPerMile,
    string PaperlessSyncStatus,
    string? PaperlessSyncedAt,
    string? PaperlessSyncError,
    string YnabSyncStatus,
    string? Notes,
    string CreatedAt);

public record FillUpPageDto(
    List<FillUpDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record CreateFillUpRequest(
    Guid VehicleId,
    string Date,
    int OdometerMiles,
    decimal Gallons,
    decimal PricePerGallon,
    decimal? TotalCost,
    short? OctaneRating,
    string StationName,
    string? StationAddress,
    decimal? Latitude,
    decimal? Longitude,
    string? Notes);

public record StatsDto(
    int TotalFillUps,
    decimal TotalGallons,
    decimal TotalCost,
    int TotalMiles,
    decimal? AvgMpg,
    decimal? AvgPricePerGallon,
    decimal? AvgCostPerFillUp,
    decimal? CostPerMile);
