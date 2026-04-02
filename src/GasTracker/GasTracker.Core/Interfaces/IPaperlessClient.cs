namespace GasTracker.Core.Interfaces;

public interface IPaperlessClient
{
    Task<int> UploadDocumentAsync(Stream content, string fileName, string title, DateOnly date, IEnumerable<string> tags);
    Task<int> EnsureTagExistsAsync(string tagName);
    Task<bool> IsHealthyAsync();
}
