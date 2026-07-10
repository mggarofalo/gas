using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace GasTracker.Tests.Integration;

public class FillUpReceiptEndpointTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public FillUpReceiptEndpointTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private static async Task<string> CreateVehicleAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2022,
            make = "Toyota",
            model = "Sienna",
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetString()!;
    }

    private static MultipartFormDataContent BuildFillUpForm(string vehicleId)
    {
        return new MultipartFormDataContent
        {
            { new StringContent(vehicleId), "vehicleId" },
            { new StringContent("2026-07-01"), "date" },
            { new StringContent("12345"), "odometerMiles" },
            { new StringContent("10.5"), "gallons" },
            { new StringContent("3.499"), "pricePerGallon" },
            { new StringContent("Test Station"), "stationName" },
        };
    }

    private static async Task<JsonElement> CreateFillUpAsync(HttpClient client, string vehicleId)
    {
        var response = await client.PostAsync("/api/fill-ups", BuildFillUpForm(vehicleId));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static MultipartFormDataContent BuildReceiptForm(
        string contentType = "image/jpeg", string fileName = "receipt.jpg", int sizeBytes = 128)
    {
        var fileContent = new ByteArrayContent(new byte[sizeBytes]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { fileContent, "receipt", fileName } };
    }

    [Fact]
    public async Task PutReceipt_OnFillUpWithoutReceipt_Returns200WithReceiptUrlAndPendingSync()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var vehicleId = await CreateVehicleAsync(client);
        var fillUp = await CreateFillUpAsync(client, vehicleId);
        var id = fillUp.GetProperty("id").GetString();

        var response = await client.PutAsync($"/api/fill-ups/{id}/receipt", BuildReceiptForm());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("receiptUrl").GetString().Should().Be($"/api/fill-ups/{id}/receipt");
        body.GetProperty("paperlessSyncStatus").GetString().Should().Be("pending");
    }

    [Fact]
    public async Task PutReceipt_ReplacesExistingReceipt_Returns200()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var vehicleId = await CreateVehicleAsync(client);

        var createForm = BuildFillUpForm(vehicleId);
        var firstReceipt = new ByteArrayContent(new byte[64]);
        firstReceipt.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        createForm.Add(firstReceipt, "receipt", "first.png");
        var createResponse = await client.PostAsync("/api/fill-ups", createForm);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        created.GetProperty("receiptUrl").GetString().Should().NotBeNull();

        var response = await client.PutAsync($"/api/fill-ups/{id}/receipt", BuildReceiptForm());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("receiptUrl").GetString().Should().NotBeNull();
        body.GetProperty("paperlessSyncStatus").GetString().Should().Be("pending");
    }

    [Fact]
    public async Task PutReceipt_MissingFile_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var vehicleId = await CreateVehicleAsync(client);
        var fillUp = await CreateFillUpAsync(client, vehicleId);
        var id = fillUp.GetProperty("id").GetString();

        var emptyForm = new MultipartFormDataContent { { new StringContent("x"), "unrelated" } };
        var response = await client.PutAsync($"/api/fill-ups/{id}/receipt", emptyForm);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutReceipt_DisallowedContentType_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var vehicleId = await CreateVehicleAsync(client);
        var fillUp = await CreateFillUpAsync(client, vehicleId);
        var id = fillUp.GetProperty("id").GetString();

        var response = await client.PutAsync(
            $"/api/fill-ups/{id}/receipt",
            BuildReceiptForm(contentType: "text/plain", fileName: "receipt.txt"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutReceipt_FileOverSizeLimit_Returns400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var vehicleId = await CreateVehicleAsync(client);
        var fillUp = await CreateFillUpAsync(client, vehicleId);
        var id = fillUp.GetProperty("id").GetString();

        var response = await client.PutAsync(
            $"/api/fill-ups/{id}/receipt",
            BuildReceiptForm(sizeBytes: 10 * 1024 * 1024 + 1));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutReceipt_NonexistentFillUp_Returns404()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();

        var response = await client.PutAsync(
            $"/api/fill-ups/{Guid.NewGuid()}/receipt", BuildReceiptForm());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutFillUp_WithFormData_UpdatesFields()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var vehicleId = await CreateVehicleAsync(client);
        var fillUp = await CreateFillUpAsync(client, vehicleId);
        var id = fillUp.GetProperty("id").GetString();

        var updateForm = new MultipartFormDataContent
        {
            { new StringContent("2026-07-02"), "date" },
            { new StringContent("12400"), "odometerMiles" },
            { new StringContent("11.2"), "gallons" },
            { new StringContent("3.599"), "pricePerGallon" },
            { new StringContent("Updated Station"), "stationName" },
            { new StringContent("123 Main St"), "stationAddress" },
            { new StringContent("Updated notes"), "notes" },
        };
        var response = await client.PutAsync($"/api/fill-ups/{id}", updateForm);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("date").GetString().Should().Be("2026-07-02");
        body.GetProperty("odometerMiles").GetInt32().Should().Be(12400);
        body.GetProperty("stationName").GetString().Should().Be("Updated Station");
        body.GetProperty("stationAddress").GetString().Should().Be("123 Main St");
        body.GetProperty("notes").GetString().Should().Be("Updated notes");
    }
}
