using GasTracker.Core.Interfaces;

namespace GasTracker.Core.Entities;

public class YnabImport : ITimestamped
{
    public Guid Id { get; set; }
    public required string YnabTransactionId { get; set; }
    public DateOnly Date { get; set; }
    public required string PayeeName { get; set; }
    public long AmountMilliunits { get; set; }
    public string? Memo { get; set; }

    // Pre-populated from memo parsing, editable in review UI
    public decimal? Gallons { get; set; }
    public decimal? PricePerGallon { get; set; }
    public short? OctaneRating { get; set; }
    public int? OdometerMiles { get; set; }
    public string? VehicleName { get; set; }
    public Guid? VehicleId { get; set; }

    public string Status { get; set; } = "pending";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
