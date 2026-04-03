namespace GasTracker.Core.Entities;

public class YnabCategoryCache
{
    public Guid Id { get; set; }
    public required string CategoryId { get; set; }
    public required string Name { get; set; }
    public required string CategoryGroupName { get; set; }
    public DateTimeOffset FetchedAt { get; set; }
}
