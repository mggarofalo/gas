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
    [GeneratedRegex(@"^(?<gallons>\d+\.?\d*)gal\s*@\s*\$?(?<price>\d+\.?\d*)/gal(?:,\s*(?<octane>\d+)\s*oct)?(?:,\s*(?<odometer>\d+)mi)?$")]
    private static partial Regex PushFormat();

    private static readonly short[] ValidOctanes = [87, 89, 91, 93];

    /// <summary>
    /// Parse a YNAB memo into gas fill-up fields.
    /// Tries push-sync format first, then flexible comma-separated format.
    /// Known vehicle names improve vehicle detection accuracy.
    /// </summary>
    public static ParsedMemo? Parse(string? memo, IReadOnlySet<string>? knownVehicleNames = null)
    {
        if (string.IsNullOrWhiteSpace(memo)) return null;

        // Try push-sync format first (exact match)
        var match = PushFormat().Match(memo.Trim());
        if (match.Success)
        {
            var gallons = decimal.Parse(match.Groups["gallons"].Value, CultureInfo.InvariantCulture);
            var price = decimal.Parse(match.Groups["price"].Value, CultureInfo.InvariantCulture);
            short? octane = match.Groups["octane"].Success ? short.Parse(match.Groups["octane"].Value) : null;
            int? mileage = match.Groups["odometer"].Success ? int.Parse(match.Groups["odometer"].Value) : null;

            return new ParsedMemo(null, octane, price, mileage, gallons);
        }

        // Flexible comma-separated format
        // Strip wrapping parens/brackets before splitting
        var cleaned = memo.Trim().Trim('(', ')', '[', ']');
        var parts = cleaned.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();
        if (parts.Length < 2) return null;

        string? vehicleName = null;
        short? parsedOctane = null;
        decimal? parsedPrice = null;
        int? parsedOdometer = null;

        foreach (var part in parts)
        {
            // Already classified enough? Skip
            var raw = part.TrimStart('$');

            // Try to classify this field
            if (TryClassifyOctane(raw, out var oct) && parsedOctane is null)
            {
                parsedOctane = oct;
            }
            else if (TryClassifyPrice(raw, part, out var price) && parsedPrice is null)
            {
                parsedPrice = price;
            }
            else if (TryClassifyOdometer(raw, out var odo) && parsedOdometer is null)
            {
                parsedOdometer = odo;
            }
            else if (vehicleName is null && IsLikelyVehicleName(part, knownVehicleNames))
            {
                vehicleName = part;
            }
        }

        // Must have extracted a price to be useful — real gas memos always include $/gal
        if (parsedPrice is null) return null;

        return new ParsedMemo(vehicleName, parsedOctane, parsedPrice, parsedOdometer, null);
    }

    private static bool TryClassifyOctane(string raw, out short octane)
    {
        octane = 0;
        if (!short.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
            return false;
        if (!ValidOctanes.Contains(val))
            return false;
        octane = val;
        return true;
    }

    private static bool TryClassifyPrice(string raw, string original, out decimal price)
    {
        price = 0;
        // Price starts with $ or is a decimal in the 1-10 range
        if (!decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) || val <= 0)
            return false;
        // Explicit $ prefix is a strong signal
        if (original.StartsWith('$'))
        {
            price = val;
            return true;
        }
        // Decimal in typical gas price range (1.00 - 9.999)
        if (val is > 0.50m and < 10m && raw.Contains('.'))
        {
            price = val;
            return true;
        }
        return false;
    }

    private static bool TryClassifyOdometer(string raw, out int odometer)
    {
        odometer = 0;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
            return false;
        // Odometer is typically a large number (> 1000)
        if (val > 1000)
        {
            odometer = val;
            return true;
        }
        return false;
    }

    private static bool IsLikelyVehicleName(string part, IReadOnlySet<string>? knownNames)
    {
        // If it matches a known vehicle name, strong signal
        if (knownNames is not null && knownNames.Contains(part))
            return true;
        // Otherwise: non-numeric, non-empty, not a $ value
        if (part.StartsWith('$')) return false;
        if (decimal.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out _)) return false;
        return part.Length > 0 && !part.All(char.IsDigit);
    }
}
