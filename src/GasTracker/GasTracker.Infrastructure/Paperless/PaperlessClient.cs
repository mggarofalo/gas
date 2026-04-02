using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GasTracker.Core.Interfaces;

namespace GasTracker.Infrastructure.Paperless;

public class PaperlessClient(HttpClient http) : IPaperlessClient
{
    public async Task<int> UploadDocumentAsync(Stream content, string fileName, string title, DateOnly date, IEnumerable<string> tags)
    {
        // Ensure tags exist and collect their IDs
        var tagIds = new List<int>();
        foreach (var tag in tags)
        {
            tagIds.Add(await EnsureTagExistsAsync(tag));
        }

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "document", fileName);
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(date.ToString("yyyy-MM-dd")), "created");

        foreach (var tagId in tagIds)
        {
            form.Add(new StringContent(tagId.ToString()), "tags");
        }

        using var res = await http.PostAsync("api/documents/post_document/", form);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadAsStringAsync();

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
                return idProp.GetInt32();
        }
        catch (JsonException)
        {
            // Older Paperless versions return plain text "OK"
        }

        return -1;
    }

    public async Task<int> EnsureTagExistsAsync(string tagName)
    {
        using var searchRes = await http.GetAsync($"api/tags/?name__iexact={Uri.EscapeDataString(tagName)}");
        searchRes.EnsureSuccessStatusCode();

        using var searchDoc = await JsonDocument.ParseAsync(await searchRes.Content.ReadAsStreamAsync());
        var results = searchDoc.RootElement.GetProperty("results");
        if (results.GetArrayLength() > 0)
            return results[0].GetProperty("id").GetInt32();

        using var createRes = await http.PostAsJsonAsync("api/tags/", new { name = tagName });
        createRes.EnsureSuccessStatusCode();

        using var createDoc = await JsonDocument.ParseAsync(await createRes.Content.ReadAsStreamAsync());
        return createDoc.RootElement.GetProperty("id").GetInt32();
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var res = await http.GetAsync("api/");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
