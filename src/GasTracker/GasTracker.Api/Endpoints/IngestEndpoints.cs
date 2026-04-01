using System.Globalization;
using System.Text.Json;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using GasTracker.Infrastructure.Data;

namespace GasTracker.Api.Endpoints;

public static class IngestEndpoints
{
    public static void MapIngestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ingest", async (HttpRequest request, AppDbContext db, IVehicleRepository vehicleRepo) =>
        {
            var form = await request.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest(new { error = "Missing 'file' field (YNAB CSV)" });

            var mappingsJson = form["mappings"].ToString();
            if (string.IsNullOrWhiteSpace(mappingsJson))
                return Results.BadRequest(new { error = "Missing 'mappings' field (vehicle name → UUID JSON)" });

            var dryRun = form.TryGetValue("dryRun", out var dr) &&
                         dr.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

            // Parse vehicle mappings
            VehicleMappings mappings;
            try
            {
                mappings = JsonSerializer.Deserialize<VehicleMappings>(mappingsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                if (mappings.Vehicles is null || mappings.Vehicles.Count == 0)
                    return Results.BadRequest(new { error = "Mappings must contain a non-empty 'vehicles' object" });
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = $"Invalid mappings JSON: {ex.Message}" });
            }

            // Validate all mapped vehicle UUIDs exist
            foreach (var (name, id) in mappings.Vehicles)
            {
                var vehicle = await vehicleRepo.GetByIdAsync(id);
                if (vehicle is null)
                    return Results.BadRequest(new { error = $"Vehicle UUID '{id}' for '{name}' not found" });
            }

            // Read and parse CSV
            using var reader = new StreamReader(file.OpenReadStream());
            var csvContent = await reader.ReadToEndAsync();
            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length < 2)
                return Results.BadRequest(new { error = "CSV must have a header row and at least one data row" });

            // Parse header to find column indices
            var header = ParseCsvLine(lines[0]);
            var colDate = Array.FindIndex(header, h => h.Equals("Date", StringComparison.OrdinalIgnoreCase));
            var colPayee = Array.FindIndex(header, h => h.Equals("Payee", StringComparison.OrdinalIgnoreCase));
            var colMemo = Array.FindIndex(header, h => h.Equals("Memo", StringComparison.OrdinalIgnoreCase));
            var colAmount = Array.FindIndex(header, h =>
                h.Equals("Amount", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Outflow", StringComparison.OrdinalIgnoreCase));

            if (colDate < 0 || colPayee < 0 || colMemo < 0 || colAmount < 0)
                return Results.BadRequest(new { error = $"CSV must have Date, Payee, Memo, and Amount/Outflow columns. Found: [{string.Join(", ", header)}]" });

            var imported = 0;
            var skipped = 0;
            var failed = 0;
            var errors = new List<string>();
            var warnings = new List<string>();
            var total = lines.Length - 1;

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) { total--; continue; }

                var row = ParseCsvLine(line);
                var rowNum = i + 1;

                try
                {
                    if (row.Length <= Math.Max(Math.Max(colDate, colPayee), Math.Max(colMemo, colAmount)))
                    {
                        errors.Add($"Row {rowNum}: not enough columns");
                        failed++;
                        continue;
                    }

                    var dateStr = row[colDate].Trim();
                    var payee = row[colPayee].Trim();
                    var memo = row[colMemo].Trim();
                    var amountStr = row[colAmount].Trim().Replace("$", "").Replace(",", "");

                    if (string.IsNullOrWhiteSpace(memo))
                    {
                        errors.Add($"Row {rowNum}: empty memo");
                        failed++;
                        continue;
                    }

                    if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                    {
                        errors.Add($"Row {rowNum}: cannot parse amount '{row[colAmount].Trim()}'");
                        failed++;
                        continue;
                    }

                    var parsed = ParseMemo(memo);
                    if (parsed is null)
                    {
                        errors.Add($"Row {rowNum}: cannot parse memo '{memo}' — expected format: vehicle, octane, $price, mileage");
                        failed++;
                        continue;
                    }

                    var (vehicleName, octane, price, mileage) = parsed.Value;

                    if (!mappings.Vehicles.TryGetValue(vehicleName, out var vehicleId))
                    {
                        errors.Add($"Row {rowNum}: unmapped vehicle '{vehicleName}'");
                        failed++;
                        continue;
                    }

                    var date = DateOnly.Parse(dateStr, CultureInfo.InvariantCulture);
                    var totalCost = Math.Abs(amount);
                    var gallons = price > 0 ? Math.Round(totalCost / price, 3) : 0m;

                    // Warnings
                    if (gallons > 50) warnings.Add($"Row {rowNum}: gallons={gallons} (>50)");
                    if (price < 1.00m) warnings.Add($"Row {rowNum}: price=${price} (<$1.00)");
                    if (price > 10.00m) warnings.Add($"Row {rowNum}: price=${price} (>$10.00)");

                    // Dedup check
                    var exists = await db.FillUps.AnyAsync(f =>
                        f.VehicleId == vehicleId && f.Date == date && f.OdometerMiles == mileage);
                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    if (!dryRun)
                    {
                        var fillUp = new FillUp
                        {
                            VehicleId = vehicleId,
                            Date = date,
                            OdometerMiles = mileage,
                            Gallons = gallons,
                            PricePerGallon = price,
                            TotalCost = totalCost,
                            OctaneRating = octane,
                            StationName = payee,
                        };
                        db.FillUps.Add(fillUp);
                    }

                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNum}: {ex.Message}");
                    failed++;
                }
            }

            if (!dryRun && imported > 0)
                await db.SaveChangesAsync();

            return Results.Ok(new
            {
                total,
                imported,
                skipped,
                failed,
                dryRun,
                errors,
                warnings,
            });
        }).RequireAuthorization().DisableAntiforgery();
    }

    public static (string vehicleName, short octane, decimal price, int mileage)? ParseMemo(string memo)
    {
        var parts = memo.Split(',');
        if (parts.Length < 4) return null;

        var vehicleName = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(vehicleName)) return null;

        if (!short.TryParse(parts[1].Trim(), out var octane)) return null;

        var priceStr = parts[2].Trim().TrimStart('$');
        if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            return null;

        if (!int.TryParse(parts[3].Trim(), out var mileage) || mileage <= 0) return null;

        return (vehicleName, octane, price, mileage);
    }

    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(field.ToString());
                field.Clear();
            }
            else
            {
                field.Append(c);
            }
        }

        fields.Add(field.ToString());
        return fields.ToArray();
    }

    private record VehicleMappings
    {
        public Dictionary<string, Guid> Vehicles { get; init; } = new();
    }
}
