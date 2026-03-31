using FluentValidation;
using GasTracker.Core.DTOs;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;

namespace GasTracker.Api.Endpoints;

public static class VehicleEndpoints
{
    public static void MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vehicles").WithTags("Vehicles").RequireAuthorization();

        group.MapGet("/", async (IVehicleRepository repo, bool? active) =>
        {
            var vehicles = await repo.ListAsync(active ?? true);
            return Results.Ok(vehicles.Select(ToDto));
        });

        group.MapGet("/{id:guid}", async (Guid id, IVehicleRepository repo) =>
        {
            var vehicle = await repo.GetByIdAsync(id);
            return vehicle is null ? Results.NotFound() : Results.Ok(ToDto(vehicle));
        });

        group.MapPost("/", async (CreateVehicleRequest req, IVehicleRepository repo, IValidator<CreateVehicleRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var vehicle = new Vehicle
            {
                Make = req.Make,
                Model = req.Model,
                Year = req.Year,
                Notes = req.Notes
            };
            await repo.CreateAsync(vehicle);
            return Results.Created($"/api/vehicles/{vehicle.Id}", ToDto(vehicle));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateVehicleRequest req, IVehicleRepository repo, IValidator<UpdateVehicleRequest> validator) =>
        {
            var validation = await validator.ValidateAsync(req);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var vehicle = await repo.GetByIdAsync(id);
            if (vehicle is null) return Results.NotFound();

            if (req.Year.HasValue) vehicle.Year = req.Year.Value;
            if (req.Make is not null) vehicle.Make = req.Make;
            if (req.Model is not null) vehicle.Model = req.Model;
            if (req.Notes is not null) vehicle.Notes = req.Notes;

            await repo.UpdateAsync(vehicle);
            return Results.Ok(ToDto(vehicle));
        });

        group.MapDelete("/{id:guid}", async (Guid id, IVehicleRepository repo) =>
        {
            var vehicle = await repo.GetByIdAsync(id);
            if (vehicle is null) return Results.NotFound();
            vehicle.IsActive = false;
            await repo.UpdateAsync(vehicle);
            return Results.NoContent();
        });
    }

    private static VehicleDto ToDto(Vehicle v) => new(
        v.Id, v.Year, v.Make, v.Model, v.Notes, v.IsActive, v.Label, v.CreatedAt, v.UpdatedAt);
}
