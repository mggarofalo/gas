using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GasTracker.Infrastructure.Storage;

public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public S3HealthCheck(IOptions<S3Options> opts)
    {
        var o = opts.Value;
        _bucketName = o.BucketName;
        var config = new AmazonS3Config
        {
            ServiceURL = $"{(o.UseSSL ? "https" : "http")}://{o.Endpoint}",
            ForcePathStyle = true,
            AuthenticationRegion = "us-east-1",
        };
        _s3 = new AmazonS3Client(o.AccessKey, o.SecretKey, config);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.GetBucketLocationAsync(
                new GetBucketLocationRequest { BucketName = _bucketName }, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy("S3 storage is unreachable");
        }
    }
}
