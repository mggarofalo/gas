using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GasTracker.Tests.Integration;

public class ReceiptReplacementEndpointTests
{
    private static readonly byte[] OriginalReceipt = [1, 2, 3, 4];
    private static readonly byte[] ReplacementReceipt = [5, 6, 7, 8, 9];

    [Theory]
    [InlineData("")]
    [InlineData("/receipt")]
    public async Task ReplacementUploadFailure_KeepsOriginalReceiptRetrievable(string endpointSuffix)
    {
        var store = new ReceiptStore();
        using var factory = new ReceiptWebAppFactory(store);
        using var client = await factory.CreateAuthenticatedClientAsync();
        var id = await CreateFillUpWithReceiptAsync(client);
        var oldPath = await GetReceiptPathAsync(factory, id);
        store.FailUpload = true;

        using var form = BuildReceiptForm("replacement.jpg", ReplacementReceipt);
        var update = () => client.PutAsync($"/api/fill-ups/{id}{endpointSuffix}", form);

        await update.Should().ThrowAsync<IOException>().WithMessage("Upload failed");
        (await GetReceiptPathAsync(factory, id)).Should().Be(oldPath);
        store.DeleteAttempts.Should().BeEmpty();
        await AssertReceiptAsync(client, id, OriginalReceipt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/receipt")]
    public async Task ReplacementSaveFailure_KeepsOriginalPathAndReceipt(string endpointSuffix)
    {
        var store = new ReceiptStore();
        using var factory = new ReceiptWebAppFactory(store);
        using var client = await factory.CreateAuthenticatedClientAsync();
        var id = await CreateFillUpWithReceiptAsync(client);
        var oldPath = await GetReceiptPathAsync(factory, id);
        factory.FailSave = true;

        using var form = BuildReceiptForm("replacement.jpg", ReplacementReceipt);
        var update = () => client.PutAsync($"/api/fill-ups/{id}{endpointSuffix}", form);

        await update.Should().ThrowAsync<DbUpdateException>().WithMessage("Save failed");
        (await GetReceiptPathAsync(factory, id)).Should().Be(oldPath);
        store.DeleteAttempts.Should().BeEmpty();
        await AssertReceiptAsync(client, id, OriginalReceipt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/receipt")]
    public async Task SameFilenameReplacement_KeepsNewReceiptRetrievable(string endpointSuffix)
    {
        var store = new ReceiptStore();
        using var factory = new ReceiptWebAppFactory(store);
        using var client = await factory.CreateAuthenticatedClientAsync();
        var id = await CreateFillUpWithReceiptAsync(client);
        var oldPath = await GetReceiptPathAsync(factory, id);

        using var form = BuildReceiptForm("original.jpg", ReplacementReceipt);
        using var response = await client.PutAsync($"/api/fill-ups/{id}{endpointSuffix}", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetReceiptPathAsync(factory, id)).Should().Be(oldPath);
        store.DeleteAttempts.Should().BeEmpty();
        await AssertReceiptAsync(client, id, ReplacementReceipt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/receipt")]
    public async Task DifferentFilenameReplacement_SavesNewPathBeforeDeletingOldReceipt(string endpointSuffix)
    {
        var store = new ReceiptStore();
        using var factory = new ReceiptWebAppFactory(store);
        using var client = await factory.CreateAuthenticatedClientAsync();
        var id = await CreateFillUpWithReceiptAsync(client);
        var oldPath = await GetReceiptPathAsync(factory, id);
        string? pathAtDelete = null;
        store.BeforeDelete = async () => pathAtDelete = await GetReceiptPathAsync(factory, id);

        using var form = BuildReceiptForm("replacement.jpg", ReplacementReceipt);
        using var response = await client.PutAsync($"/api/fill-ups/{id}{endpointSuffix}", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newPath = await GetReceiptPathAsync(factory, id);
        newPath.Should().NotBe(oldPath);
        pathAtDelete.Should().Be(newPath);
        store.DeleteAttempts.Should().Equal(oldPath);
        store.Objects.Should().NotContainKey(oldPath!);
        await AssertReceiptAsync(client, id, ReplacementReceipt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/receipt")]
    public async Task OldReceiptCleanupFailure_StillReturnsSuccessWithRetrievableReplacement(string endpointSuffix)
    {
        var store = new ReceiptStore();
        using var factory = new ReceiptWebAppFactory(store);
        using var client = await factory.CreateAuthenticatedClientAsync();
        var id = await CreateFillUpWithReceiptAsync(client);
        var oldPath = await GetReceiptPathAsync(factory, id);
        store.FailDelete = true;

        using var form = BuildReceiptForm("replacement.jpg", ReplacementReceipt);
        using var response = await client.PutAsync($"/api/fill-ups/{id}{endpointSuffix}", form);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetReceiptPathAsync(factory, id)).Should().NotBe(oldPath);
        store.DeleteAttempts.Should().Equal(oldPath);
        store.Objects.Should().ContainKey(oldPath!);
        await AssertReceiptAsync(client, id, ReplacementReceipt);
    }

    private static async Task<Guid> CreateFillUpWithReceiptAsync(HttpClient client)
    {
        using var vehicleResponse = await client.PostAsJsonAsync("/api/vehicles", new
        {
            year = (short)2022,
            make = "Toyota",
            model = "Sienna",
        });
        vehicleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<JsonElement>();

        using var form = BuildReceiptForm("original.jpg", OriginalReceipt);
        form.Add(new StringContent(vehicle.GetProperty("id").GetString()!), "vehicleId");
        form.Add(new StringContent("2026-07-01"), "date");
        form.Add(new StringContent("12345"), "odometerMiles");
        form.Add(new StringContent("10.5"), "gallons");
        form.Add(new StringContent("3.499"), "pricePerGallon");
        form.Add(new StringContent("Test Station"), "stationName");
        using var response = await client.PostAsync("/api/fill-ups", form);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var fillUp = await response.Content.ReadFromJsonAsync<JsonElement>();
        return fillUp.GetProperty("id").GetGuid();
    }

    private static MultipartFormDataContent BuildReceiptForm(string fileName, byte[] bytes)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        return new MultipartFormDataContent { { content, "receipt", fileName } };
    }

    private static async Task<string?> GetReceiptPathAsync(TestWebAppFactory factory, Guid id)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return (await db.FillUps.FindAsync(id))!.ReceiptPath;
    }

    private static async Task AssertReceiptAsync(HttpClient client, Guid id, byte[] expectedBytes)
    {
        using var response = await client.GetAsync($"/api/fill-ups/{id}/receipt");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(expectedBytes);
    }

    private sealed class ReceiptWebAppFactory(ReceiptStore store) : TestWebAppFactory
    {
        public bool FailSave { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IReceiptStore>();
                services.AddSingleton<IReceiptStore>(store);

                var optionsRegistration = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                services.Remove(optionsRegistration);
                services.AddSingleton(sp =>
                {
                    var options = (DbContextOptions<AppDbContext>)optionsRegistration.ImplementationFactory!(sp);
                    return new DbContextOptionsBuilder<AppDbContext>(options)
                        .AddInterceptors(new SaveFailureInterceptor(() => FailSave)).Options;
                });
            });
        }
    }

    private sealed class SaveFailureInterceptor(Func<bool> shouldFail) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (shouldFail()) throw new DbUpdateException("Save failed");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    // Model the production filename-based keys, including in-place overwrites.
    private sealed class ReceiptStore : IReceiptStore
    {
        public Dictionary<string, byte[]> Objects { get; } = [];
        public List<string> DeleteAttempts { get; } = [];
        public bool FailUpload { get; set; }
        public bool FailDelete { get; set; }
        public Func<Task>? BeforeDelete { get; set; }

        public async Task<string> UploadAsync(
            Guid vehicleId, Guid fillUpId, string fileName, string contentType, Stream content)
        {
            if (FailUpload) throw new IOException("Upload failed");
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer);
            var key = $"{vehicleId}/{fillUpId}/{fileName}";
            Objects[key] = buffer.ToArray();
            return key;
        }

        public Task<Stream> DownloadAsync(string objectKey)
            => Task.FromResult<Stream>(new MemoryStream(Objects[objectKey]));

        public async Task DeleteAsync(string objectKey)
        {
            DeleteAttempts.Add(objectKey);
            if (BeforeDelete is not null) await BeforeDelete();
            if (FailDelete) throw new IOException("Delete failed");
            Objects.Remove(objectKey);
        }

        public Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry)
            => Task.FromResult($"https://example.com/{objectKey}");

        public Task EnsureBucketExistsAsync() => Task.CompletedTask;
    }
}
