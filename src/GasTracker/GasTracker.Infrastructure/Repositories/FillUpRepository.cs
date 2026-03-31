using GasTracker.Core.DTOs;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Infrastructure.Repositories;

public class FillUpRepository(AppDbContext db) : IFillUpRepository
{
    public async Task<(List<FillUp> Items, int TotalCount)> ListAsync(
        Guid? vehicleId, DateOnly? startDate, DateOnly? endDate,
        int page, int pageSize, string sortBy, string sortDir)
    {
        var query = db.FillUps.Include(f => f.Vehicle).AsQueryable();

        if (vehicleId.HasValue)
            query = query.Where(f => f.VehicleId == vehicleId.Value);
        if (startDate.HasValue)
            query = query.Where(f => f.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(f => f.Date <= endDate.Value);

        var totalCount = await query.CountAsync();

        query = (sortBy?.ToLowerInvariant(), sortDir?.ToLowerInvariant()) switch
        {
            ("date", "asc") => query.OrderBy(f => f.Date),
            ("odometer", "desc") => query.OrderByDescending(f => f.OdometerMiles),
            ("odometer", "asc") => query.OrderBy(f => f.OdometerMiles),
            ("gallons", "desc") => query.OrderByDescending(f => f.Gallons),
            ("gallons", "asc") => query.OrderBy(f => f.Gallons),
            ("total", "desc") => query.OrderByDescending(f => f.TotalCost),
            ("total", "asc") => query.OrderBy(f => f.TotalCost),
            _ => query.OrderByDescending(f => f.Date),
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<FillUp?> GetByIdAsync(Guid id)
        => await db.FillUps.Include(f => f.Vehicle).FirstOrDefaultAsync(f => f.Id == id);

    public async Task<FillUp> CreateAsync(FillUp fillUp)
    {
        db.FillUps.Add(fillUp);
        await db.SaveChangesAsync();
        return fillUp;
    }

    public async Task UpdateAsync(FillUp fillUp)
    {
        db.FillUps.Update(fillUp);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(FillUp fillUp)
    {
        db.FillUps.Remove(fillUp);
        await db.SaveChangesAsync();
    }

    public async Task<int?> GetTripMilesAsync(FillUp fillUp)
    {
        var previousOdometer = await db.FillUps
            .Where(f => f.VehicleId == fillUp.VehicleId && f.Id != fillUp.Id)
            .Where(f => f.Date < fillUp.Date || (f.Date == fillUp.Date && f.OdometerMiles < fillUp.OdometerMiles))
            .OrderByDescending(f => f.Date)
            .ThenByDescending(f => f.OdometerMiles)
            .Select(f => (int?)f.OdometerMiles)
            .FirstOrDefaultAsync();

        return previousOdometer.HasValue ? fillUp.OdometerMiles - previousOdometer.Value : null;
    }

    public async Task<StatsDto> GetStatsAsync(Guid? vehicleId, DateOnly? startDate, DateOnly? endDate)
    {
        var query = db.FillUps.AsQueryable();
        if (vehicleId.HasValue) query = query.Where(f => f.VehicleId == vehicleId.Value);
        if (startDate.HasValue) query = query.Where(f => f.Date >= startDate.Value);
        if (endDate.HasValue) query = query.Where(f => f.Date <= endDate.Value);

        var fillUps = await query.ToListAsync();

        if (fillUps.Count == 0)
            return new StatsDto(0, 0, 0, 0, null, null, null, null);

        var totalGallons = fillUps.Sum(f => f.Gallons);
        var totalCost = fillUps.Sum(f => f.TotalCost);
        var totalFillUps = fillUps.Count;

        // Compute total miles per vehicle as max(odometer) - min(odometer)
        var totalMiles = fillUps
            .GroupBy(f => f.VehicleId)
            .Sum(g => g.Max(f => f.OdometerMiles) - g.Min(f => f.OdometerMiles));

        var avgMpg = totalMiles > 0 && totalGallons > 0 ? Math.Round((decimal)totalMiles / totalGallons, 1) : (decimal?)null;
        var avgPrice = Math.Round(totalGallons > 0 ? fillUps.Average(f => f.PricePerGallon) : 0, 3);
        var avgCost = Math.Round(totalCost / totalFillUps, 2);
        var costPerMile = totalMiles > 0 ? Math.Round(totalCost / totalMiles, 2) : (decimal?)null;

        return new StatsDto(totalFillUps, totalGallons, totalCost, totalMiles, avgMpg, avgPrice, avgCost, costPerMile);
    }
}
