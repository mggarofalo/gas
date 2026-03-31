using GasTracker.Core.Interfaces;

namespace GasTracker.Core.Entities;

public class Vehicle : ITimestamped
{
    public Guid Id { get; set; }
    public required short Year { get; set; }
    public required string Make { get; set; }
    public required string Model { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<FillUp> FillUps { get; set; } = [];

    public string Label => $"{Year} {Make} {Model}";
}
