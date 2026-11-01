using GasTracker.Core.Interfaces;

namespace GasTracker.Core.Entities;

/// <summary>
/// Tenant boundary: members share every vehicle, fill-up, and integration setting in their household.
/// </summary>
public class Household : ITimestamped
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ApplicationUser> Members { get; set; } = [];
    public ICollection<Vehicle> Vehicles { get; set; } = [];
}
