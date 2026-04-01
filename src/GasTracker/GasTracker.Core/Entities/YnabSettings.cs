using GasTracker.Core.Interfaces;

namespace GasTracker.Core.Entities;

public class YnabSettings : ITimestamped
{
    public Guid Id { get; set; }
    public required string ApiToken { get; set; }
    public string? PlanId { get; set; }
    public string? PlanName { get; set; }
    public string? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
