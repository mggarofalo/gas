namespace GasTracker.Core.DTOs;

public record VehicleDto(
    Guid Id,
    short Year,
    string Make,
    string Model,
    string? Notes,
    short? OctaneRating,
    bool IsActive,
    string Label,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateVehicleRequest(
    short Year,
    string Make,
    string Model,
    string? Notes,
    short? OctaneRating);

public record UpdateVehicleRequest(
    short? Year,
    string? Make,
    string? Model,
    string? Notes,
    short? OctaneRating);
