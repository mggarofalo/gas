namespace GasTracker.Core.Interfaces;

public interface IReceiptStore
{
    Task<string> UploadAsync(Guid vehicleId, Guid fillUpId, string fileName, string contentType, Stream content);
    Task<Stream> DownloadAsync(string objectKey);
    Task DeleteAsync(string objectKey);
    Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiry);
    Task EnsureBucketExistsAsync();
}
