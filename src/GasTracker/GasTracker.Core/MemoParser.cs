using System.Globalization;

namespace GasTracker.Core;

public static class MemoParser
{
    public static (string VehicleName, short Octane, decimal Price, int Mileage)? Parse(string memo)
    {
        var parts = memo.Split(',');
        if (parts.Length < 4) return null;

        var vehicleName = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(vehicleName)) return null;

        if (!short.TryParse(parts[1].Trim(), out var octane) || octane <= 0) return null;

        var priceStr = parts[2].Trim().TrimStart('$');
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            return null;

        if (!int.TryParse(parts[3].Trim(), out var mileage) || mileage <= 0) return null;

        return (vehicleName, octane, price, mileage);
    }
}
