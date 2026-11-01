using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Api;

/// <summary>
/// Resolves the authenticated caller's household so creates can be stamped with it.
/// Interim lookup until the JWT carries a household claim (GAS-98).
/// </summary>
public static class CallerHousehold
{
    public static async Task<Guid?> GetCallerHouseholdIdAsync(this AppDbContext db, ClaimsPrincipal caller)
    {
        var userId = caller.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? caller.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
            return null;

        return await db.Users
            .Where(u => u.Id == userId)
            .Select(u => (Guid?)u.HouseholdId)
            .FirstOrDefaultAsync();
    }
}
