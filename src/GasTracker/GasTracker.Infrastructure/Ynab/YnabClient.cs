using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GasTracker.Core.Interfaces;

namespace GasTracker.Infrastructure.Ynab;

public class YnabClient(HttpClient http) : IYnabClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<List<YnabPlan>> GetPlansAsync(string token)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/v1/plans");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await SendAsync(req);
        var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        var plans = doc.RootElement.GetProperty("data").GetProperty("plans");

        return plans.EnumerateArray().Select(p => new YnabPlan(
            p.GetProperty("id").GetString()!,
            p.GetProperty("name").GetString()!,
            p.TryGetProperty("last_modified_on", out var lm) ? lm.GetString() : null
        )).ToList();
    }

    public async Task<List<YnabAccount>> GetAccountsAsync(string token, string planId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/v1/plans/{planId}/accounts");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await SendAsync(req);
        var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        var accounts = doc.RootElement.GetProperty("data").GetProperty("accounts");

        return accounts.EnumerateArray()
            .Where(a =>
                a.GetProperty("on_budget").GetBoolean() &&
                !a.GetProperty("closed").GetBoolean() &&
                !a.GetProperty("deleted").GetBoolean())
            .Select(a => new YnabAccount(
                a.GetProperty("id").GetString()!,
                a.GetProperty("name").GetString()!,
                a.GetProperty("type").GetString()!,
                a.GetProperty("balance").GetInt64()
            )).ToList();
    }

    public async Task<List<YnabCategory>> GetCategoriesAsync(string token, string planId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/v1/plans/{planId}/categories");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await SendAsync(req);
        var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        var groups = doc.RootElement.GetProperty("data").GetProperty("category_groups");

        var categories = new List<YnabCategory>();
        foreach (var group in groups.EnumerateArray())
        {
            if (group.GetProperty("hidden").GetBoolean() || group.GetProperty("deleted").GetBoolean())
                continue;

            var groupName = group.GetProperty("name").GetString()!;
            foreach (var cat in group.GetProperty("categories").EnumerateArray())
            {
                if (cat.GetProperty("hidden").GetBoolean() || cat.GetProperty("deleted").GetBoolean())
                    continue;

                categories.Add(new YnabCategory(
                    cat.GetProperty("id").GetString()!,
                    cat.GetProperty("name").GetString()!,
                    groupName
                ));
            }
        }
        return categories;
    }

    public async Task<YnabTransactionResult> CreateTransactionAsync(string token, string planId, YnabTransaction tx)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/v1/plans/{planId}/transactions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(new { transaction = tx }, options: JsonOpts);

        var res = await SendAsync(req, allowConflict: true);

        if (res.StatusCode == HttpStatusCode.Conflict)
            return new YnabTransactionResult(null, IsDuplicate: true);

        var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        var data = doc.RootElement.GetProperty("data");
        var txIds = data.GetProperty("transaction_ids");
        var id = txIds.GetArrayLength() > 0 ? txIds[0].GetString() : null;

        return new YnabTransactionResult(id, IsDuplicate: false);
    }

    public async Task<YnabTransactionPage> GetTransactionsAsync(string token, string planId, string? accountId = null, DateOnly? sinceDate = null, long? lastServerKnowledge = null)
    {
        var url = string.IsNullOrWhiteSpace(accountId)
            ? $"/v1/plans/{planId}/transactions"
            : $"/v1/plans/{planId}/accounts/{accountId}/transactions";
        var queryParams = new List<string>();
        if (sinceDate.HasValue)
            queryParams.Add($"since_date={sinceDate.Value:yyyy-MM-dd}");
        if (lastServerKnowledge.HasValue)
            queryParams.Add($"last_knowledge_of_server={lastServerKnowledge.Value}");
        if (queryParams.Count > 0)
            url += "?" + string.Join("&", queryParams);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await SendAsync(req);
        var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        var data = doc.RootElement.GetProperty("data");
        var transactions = data.GetProperty("transactions");
        var serverKnowledge = data.GetProperty("server_knowledge").GetInt64();

        var list = transactions.EnumerateArray()
            .Where(t => !t.GetProperty("deleted").GetBoolean())
            .Select(t => new YnabTransactionRead(
                t.GetProperty("id").GetString()!,
                t.GetProperty("date").GetString()!,
                t.GetProperty("amount").GetInt64(),
                t.TryGetProperty("payee_name", out var pn) ? pn.GetString() : null,
                t.TryGetProperty("memo", out var m) ? m.GetString() : null,
                t.TryGetProperty("category_id", out var ci) ? ci.GetString() : null,
                t.GetProperty("account_id").GetString()!,
                t.TryGetProperty("import_id", out var ii) ? ii.GetString() : null
            )).ToList();

        return new YnabTransactionPage(list, serverKnowledge);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage req, bool allowConflict = false)
    {
        var res = await http.SendAsync(req);

        if (res.StatusCode == HttpStatusCode.Unauthorized)
            throw new InvalidOperationException("YNAB API token is invalid or revoked");

        if (res.StatusCode == HttpStatusCode.TooManyRequests)
            throw new InvalidOperationException("YNAB API rate limit exceeded (200 requests/hour)");

        if (allowConflict && res.StatusCode == HttpStatusCode.Conflict)
            return res;

        res.EnsureSuccessStatusCode();
        return res;
    }
}
