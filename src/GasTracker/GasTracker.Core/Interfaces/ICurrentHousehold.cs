namespace GasTracker.Core.Interfaces;

/// <summary>
/// The household the current caller belongs to. Drives the EF global query filters and
/// stamps creates, so a request can only see and write its own household's data.
/// </summary>
public interface ICurrentHousehold
{
    /// <summary>
    /// True while handling an HTTP request: reads are filtered to <see cref="HouseholdId"/>.
    /// False at startup and in background services, which work across households.
    /// </summary>
    bool IsScoped { get; }

    /// <summary>
    /// The caller's household from the <see cref="HouseholdClaimTypes.HouseholdId"/> claim,
    /// or null when the request carries none (a scoped request then matches no rows).
    /// </summary>
    Guid? HouseholdId { get; }
}

public static class HouseholdClaimTypes
{
    public const string HouseholdId = "household_id";
}
