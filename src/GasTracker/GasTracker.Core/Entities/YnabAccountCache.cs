namespace GasTracker.Core.Entities;

public class YnabAccountCache
{
    public Guid Id { get; set; }
    public required string AccountId { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public long Balance { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}
