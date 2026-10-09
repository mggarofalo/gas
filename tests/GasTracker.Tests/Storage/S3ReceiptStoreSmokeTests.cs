using System.Text;
using FluentAssertions;
using GasTracker.Infrastructure.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GasTracker.Tests.Storage;

public class S3ReceiptStoreSmokeTests
{
    [S3MockFact]
    public async Task ReceiptOperations_AndHealthCheck_WorkAgainstS3Mock()
    {
        var endpoint = new Uri(Environment.GetEnvironmentVariable("S3MOCK_TEST_ENDPOINT")!);
        var options = Options.Create(new S3Options
        {
            Endpoint = endpoint.Authority,
            UseSSL = endpoint.Scheme == "https",
            BucketName = $"gas-sdk-test-{Guid.NewGuid():N}",
        });
        var store = new S3ReceiptStore(options);
        await store.EnsureBucketExistsAsync();
        var bytes = Encoding.UTF8.GetBytes("Receipt bytes, with a newline\n");
        using var input = new MemoryStream(bytes);
        var key = await store.UploadAsync(Guid.NewGuid(), Guid.NewGuid(), "receipt café #1.jpg", "image/jpeg", input);
        using var downloaded = await store.DownloadAsync(key);
        using var actual = new MemoryStream();
        await downloaded.CopyToAsync(actual);
        actual.ToArray().Should().Equal(bytes);
        (await new S3HealthCheck(options).CheckHealthAsync(new HealthCheckContext())).Status.Should().Be(HealthStatus.Healthy);
        await store.DeleteAsync(key);
    }
}

public sealed class S3MockFactAttribute : FactAttribute
{
    public S3MockFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("S3MOCK_TEST_ENDPOINT")))
            Skip = "Run tests/migration/run.sh to test against a real S3Mock service.";
    }
}
