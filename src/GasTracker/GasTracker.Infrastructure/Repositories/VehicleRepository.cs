using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Infrastructure.Repositories;

public class VehicleRepository(AppDbContext db) : IVehicleRepository
{
    public async Task<List<Vehicle>> ListAsync(bool activeOnly = true)
    {
        var query = db.Vehicles.AsQueryable();
        if (activeOnly)
            query = query.Where(v => v.IsActive);
        return await query.OrderBy(v => v.Year).ThenBy(v => v.Make).ThenBy(v => v.Model).ToListAsync();
    }

    public async Task<Vehicle?> GetByIdAsync(Guid id)
        => await db.Vehicles.FindAsync(id);

    public async Task<Vehicle> CreateAsync(Vehicle vehicle)
    {
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    public async Task UpdateAsync(Vehicle vehicle)
    {
        db.Vehicles.Update(vehicle);
        await db.SaveChangesAsync();
    }
}
