namespace GasTracker.Infrastructure.Paperless;

public class PaperlessOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string Token { get; set; } = "";
    public bool Enabled { get; set; }
    public int PollIntervalSeconds { get; set; } = 30;
}
