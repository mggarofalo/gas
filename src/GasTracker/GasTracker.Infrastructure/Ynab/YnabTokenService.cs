using GasTracker.Infrastructure.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Infrastructure.Ynab;

public class YnabTokenService(AppDbContext db, IDataProtectionProvider dp)
{
    private const string Purpose = "YnabApiToken";

    public async Task<string> GetDecryptedTokenAsync()
    {
        var settings = await db.YnabSettings.FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("YNAB is not configured");

        try
        {
            var protector = dp.CreateProtector(Purpose);
            return protector.Unprotect(settings.ApiToken);
        }
        catch
        {
            throw new InvalidOperationException("Failed to decrypt YNAB API token");
        }
    }
}
