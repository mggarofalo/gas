using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace GasTracker.Infrastructure.Storage;

public class MinioHealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucketName;

    public MinioHealthCheck(IOptions<MinioOptions> opts)
    {
        var o = opts.Value;
        _bucketName = o.BucketName;
        var config = new AmazonS3Config
        {
            ServiceURL = $"{(o.UseSSL ? "https" : "http")}://{o.Endpoint}",
            ForcePathStyle = true,
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
            return HealthCheckResult.Unhealthy("MinIO is unreachable");
        }
    }
}
