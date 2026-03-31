using GasTracker.Core.Entities;

namespace GasTracker.Core.Interfaces;

public interface IVehicleRepository
{
    Task<List<Vehicle>> ListAsync(bool activeOnly = true);
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task<Vehicle> CreateAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
}
