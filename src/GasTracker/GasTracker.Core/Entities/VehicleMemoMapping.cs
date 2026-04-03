using GasTracker.Core.Interfaces;

namespace GasTracker.Core.Entities;

public class VehicleMemoMapping : ITimestamped
{
    public Guid Id { get; set; }
    public required string MemoName { get; set; }
    public Guid VehicleId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
