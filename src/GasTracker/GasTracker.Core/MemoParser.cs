using System.Globalization;
using System.Text.RegularExpressions;

namespace GasTracker.Core;

public record ParsedMemo(
    string? VehicleName,
    short? OctaneRating,
    decimal? PricePerGallon,
    int? OdometerMiles,
    decimal? Gallons);

public static partial class MemoParser
{
    // Push-sync format: "14.500gal @ $3.500/gal, 87 oct, 45000mi"
    [GeneratedRegex(@"^(\d+\.?\d*)gal\s*@\s*\$(\d+\.?\d*)/gal(?:,\s*(\d+)\s*oct)?(?:,\s*(\d+)mi)?$")]
    private static partial Regex PushFormat();

    /// <summary>
    /// Tries push-sync format first, then falls back to ingest format (vehicle, octane, $price, mileage).
    /// </summary>
    public static ParsedMemo? Parse(string? memo)
    {
        if (string.IsNullOrWhiteSpace(memo)) return null;

        // Try push-sync format
        var match = PushFormat().Match(memo.Trim());
        if (match.Success)
        {
            var gallons = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            var price = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            short? octane = match.Groups[3].Success ? short.Parse(match.Groups[3].Value) : null;
            int? mileage = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : null;

            return new ParsedMemo(null, octane, price, mileage, gallons);
        }

        // Try ingest format: vehicle, octane, $price, mileage
        var parts = memo.Split(',');
        if (parts.Length < 4) return null;

        var vehicleName = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(vehicleName)) return null;

        if (!short.TryParse(parts[1].Trim(), out var ingestOctane) || ingestOctane <= 0) return null;

        var priceStr = parts[2].Trim().TrimStart('$');
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var ingestPrice) || ingestPrice <= 0)
            return null;

        if (!int.TryParse(parts[3].Trim(), out var ingestMileage) || ingestMileage <= 0) return null;

        return new ParsedMemo(vehicleName, ingestOctane, ingestPrice, ingestMileage, null);
    }
}
