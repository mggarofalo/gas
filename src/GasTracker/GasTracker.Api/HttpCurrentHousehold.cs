using GasTracker.Core.Interfaces;

namespace GasTracker.Api;

/// <summary>
/// Reads the caller's household from the JWT. Any HTTP request is scoped, so an anonymous
/// request or a token without the claim sees no household data rather than all of it.
/// </summary>
public class HttpCurrentHousehold(IHttpContextAccessor httpContextAccessor) : ICurrentHousehold
{
    public bool IsScoped => httpContextAccessor.HttpContext is not null;

    public Guid? HouseholdId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(HouseholdClaimTypes.HouseholdId)?.Value, out var id)
            ? id
            : null;
}
