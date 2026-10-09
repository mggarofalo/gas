using System.Net;
using System.Text.Json;
using FluentAssertions;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Ynab;

namespace GasTracker.Tests.Ynab;

public class YnabClientTests
{
    [Fact]
    public async Task CreateTransaction_OmitsImportIdAndSendsUncleared()
    {
        string? body = null;
        using var http = new HttpClient(new Handler(async request =>
        {
            body = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"data":{"transaction_ids":["remote-id"]}}"""),
            };
        })) { BaseAddress = new Uri("https://api.ynab.com") };

        var result = await new YnabClient(http).CreateTransactionAsync("token", "plan",
            new YnabTransaction("account", "2026-10-09", -36740, "Station", "category", "Memo"));

        using var json = JsonDocument.Parse(body!);
        var tx = json.RootElement.GetProperty("transaction");
        tx.TryGetProperty("import_id", out _).Should().BeFalse();
        tx.GetProperty("cleared").GetString().Should().Be("uncleared");
        tx.GetProperty("approved").GetBoolean().Should().BeTrue();
        result.TransactionId.Should().Be("remote-id");
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
