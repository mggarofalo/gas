using System.Text.RegularExpressions;

namespace GasTracker.Core;

public static partial class YnabTransactionIdentity
{
    public static string ImportId(Guid fillUpId) => $"GAS:{fillUpId:N}";
    public static string MemoMarker(Guid fillUpId) => $"[{ImportId(fillUpId)}]";

    [GeneratedRegex(@"\[GAS:[0-9a-f]{32}\]")]
    private static partial Regex MarkerPattern();

    public static bool IsGasTransaction(string? importId, string? memo) =>
        importId?.StartsWith("GAS:", StringComparison.Ordinal) == true ||
        (memo is not null && MarkerPattern().IsMatch(memo));

    public static string StripMarker(string memo) => MarkerPattern().Replace(memo, "").Trim().TrimEnd(',');
}
