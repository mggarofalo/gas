using GasTracker.Core.Interfaces;

namespace GasTracker.Core.Entities;

public class FillUp : ITimestamped
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public DateOnly Date { get; set; }
    public int OdometerMiles { get; set; }
    public decimal Gallons { get; set; }
    public decimal PricePerGallon { get; set; }
    public decimal TotalCost { get; set; }
    public required string StationName { get; set; }
    public string? StationAddress { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? ReceiptPath { get; set; }
    public string? Notes { get; set; }

    // Paperless sync state
    public string PaperlessSyncStatus { get; set; } = "none";
    public int? PaperlessDocumentId { get; set; }
    public string? PaperlessSyncError { get; set; }
    public DateTimeOffset? PaperlessSyncedAt { get; set; }
    public short PaperlessSyncAttempts { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
