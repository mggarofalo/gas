using GasTracker.Core.DTOs;
using GasTracker.Core.Entities;

namespace GasTracker.Core.Interfaces;

public interface IFillUpRepository
{
    Task<(List<FillUp> Items, int TotalCount)> ListAsync(
        Guid? vehicleId, DateOnly? startDate, DateOnly? endDate,
        int page, int pageSize, string sortBy, string sortDir);
    Task<FillUp?> GetByIdAsync(Guid id);
    Task<FillUp> CreateAsync(FillUp fillUp);
    Task UpdateAsync(FillUp fillUp);
    Task DeleteAsync(FillUp fillUp);
    Task<int?> GetTripMilesAsync(FillUp fillUp);
    Task<StatsDto> GetStatsAsync(Guid? vehicleId, DateOnly? startDate, DateOnly? endDate);
}
