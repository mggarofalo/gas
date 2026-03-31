using Amazon.S3;
using Amazon.S3.Model;
using GasTracker.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace GasTracker.Infrastructure.Storage;

public class MinioReceiptStore : IReceiptStore
{
    private readonly IAmazonS3 _s3;
    private readonly MinioOptions _opts;

    public MinioReceiptStore(IOptions<MinioOptions> opts)
    {
        _opts = opts.Value;
        var config = new AmazonS3Config
        {
            ServiceURL = $"{(_opts.UseSSL ? "https" : "http")}://{_opts.Endpoint}",
            ForcePathStyle = true,
        };
        _s3 = new AmazonS3Client(_opts.AccessKey, _opts.SecretKey, config);
    }

    public async Task EnsureBucketExistsAsync()
    {
        var buckets = await _s3.ListBucketsAsync();
        if (buckets.Buckets.All(b => b.BucketName != _opts.BucketName))
        {
            await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _opts.BucketName });
        }
    }

    public async Task<string> UploadAsync(Guid vehicleId, Guid fillUpId, string fileName, string contentType, Stream content)
    {
        var key = $"{vehicleId}/{fillUpId}/{fileName}";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _opts.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        });
        return key;
    }

    public async Task DeleteAsync(string objectKey)
    {
        await _s3.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = _opts.BucketName,
            Key = objectKey,
        });
    }

    public Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry)
    {
        var url = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _opts.BucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET,
        });
        return Task.FromResult(url);
    }
}
