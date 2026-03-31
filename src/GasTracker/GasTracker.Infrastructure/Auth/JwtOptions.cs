namespace GasTracker.Infrastructure.Auth;

public class JwtOptions
{
    public required string Key { get; set; }
    public string Issuer { get; set; } = "gas-api";
    public string Audience { get; set; } = "gas-app";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}
