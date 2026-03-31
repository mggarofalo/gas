namespace GasTracker.Infrastructure.Storage;

public class MinioOptions
{
    public required string Endpoint { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
    public string BucketName { get; set; } = "gas-receipts";
    public bool UseSSL { get; set; }
}
