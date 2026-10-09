namespace GasTracker.Infrastructure.Storage;

public class S3Options
{
    public string Endpoint { get; set; } = "s3mock:9090";
    public string AccessKey { get; set; } = "gas";
    public string SecretKey { get; set; } = "gas-local-storage";
    public string BucketName { get; set; } = "gas-receipts";
    public bool UseSSL { get; set; }
}
