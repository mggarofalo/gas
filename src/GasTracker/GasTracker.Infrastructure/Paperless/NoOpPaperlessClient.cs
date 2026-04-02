using GasTracker.Core.Interfaces;

namespace GasTracker.Infrastructure.Paperless;

public class NoOpPaperlessClient : IPaperlessClient
{
    public Task<int> UploadDocumentAsync(Stream content, string fileName, string title, DateOnly date, IEnumerable<string> tags)
    {
        Console.WriteLine("Paperless sync disabled — skipping document upload");
        return Task.FromResult(-1);
    }

    public Task<int> EnsureTagExistsAsync(string tagName)
    {
        return Task.FromResult(-1);
    }

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(false);
    }
}
