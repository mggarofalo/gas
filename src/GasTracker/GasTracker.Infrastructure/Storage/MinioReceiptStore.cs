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
        try
        {
            var buckets = await _s3.ListBucketsAsync();
            if (buckets.Buckets is null || buckets.Buckets.All(b => b.BucketName != _opts.BucketName))
            {
                await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _opts.BucketName });
            }
        }
        catch (Exception ex)
        {
            // Log but don't crash — bucket may already exist or MinIO may not be ready yet.
            // The app will retry on first upload.
            Console.WriteLine($"Warning: Could not ensure MinIO bucket exists: {ex.Message}");
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

    public async Task<Stream> DownloadAsync(string objectKey)
    {
        using var response = await _s3.GetObjectAsync(new GetObjectRequest
        {
            BucketName = _opts.BucketName,
            Key = objectKey,
        });
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
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
