using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GasTracker.Infrastructure.Storage;

public class MinioHealthCheck : IHealthCheck
{
    private readonly MinioOptions _opts;

    public MinioHealthCheck(IOptions<MinioOptions> opts)
    {
        _opts = opts.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AmazonS3Config
            {
                ServiceURL = $"{(_opts.UseSSL ? "https" : "http")}://{_opts.Endpoint}",
                ForcePathStyle = true,
            };
            using var client = new AmazonS3Client(_opts.AccessKey, _opts.SecretKey, config);
            await client.ListBucketsAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
