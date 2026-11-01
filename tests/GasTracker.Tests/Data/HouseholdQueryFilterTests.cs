using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Tests.Data;

/// <summary>
/// The household global query filters on their own: when they apply, and that they
/// translate to SQL on PostgreSQL (the in-memory provider accepts queries Npgsql may not).
/// </summary>
public class HouseholdQueryFilterTests
{
    private sealed class FixedHousehold(bool isScoped, Guid? householdId) : ICurrentHousehold
    {
        public bool IsScoped => isScoped;
        public Guid? HouseholdId => householdId;
    }

    private static async Task<(Guid A, Guid B)> SeedTwoHouseholdsAsync(AppDbContext db)
    {
        var ids = new List<Guid>();
        foreach (var tag in new[] { "a", "b" })
        {
            var household = new Household { Name = tag };
            var vehicle = new Vehicle { Household = household, Year = 2020, Make = "Make", Model = tag };
            db.AddRange(
                household,
                vehicle,
                new FillUp
                {
                    Vehicle = vehicle, Date = new DateOnly(2026, 5, 1), OdometerMiles = 1000, Gallons = 10m,
                    PricePerGallon = 3m, TotalCost = 30m, StationName = $"Station {tag}", Latitude = 40m, Longitude = -74m,
                },
                new YnabSettings { Household = household, ApiToken = tag },
                new YnabImport { Household = household, YnabTransactionId = tag, PayeeName = tag, VehicleName = "Car" },
                new VehicleMemoMapping { Household = household, MemoName = "Car", Vehicle = vehicle },
                new YnabAccountCache { Household = household, AccountId = tag, Name = tag, Type = "checking" },
                new YnabCategoryCache { Household = household, CategoryId = tag, Name = tag, CategoryGroupName = "Auto" });
            await db.SaveChangesAsync();
            ids.Add(household.Id);
        }
        return (ids[0], ids[1]);
    }

    private static async Task AssertSeesOnlyAsync(AppDbContext db, Guid householdId)
    {
        (await db.Vehicles.Select(v => v.HouseholdId).ToListAsync()).Should().Equal(householdId);
        (await db.FillUps.Include(f => f.Vehicle).Select(f => f.Vehicle.HouseholdId).ToListAsync()).Should().Equal(householdId);
        (await db.YnabSettings.Select(s => s.HouseholdId).ToListAsync()).Should().Equal(householdId);
        (await db.YnabImports.Select(i => i.HouseholdId).ToListAsync()).Should().Equal(householdId);
        (await db.VehicleMemoMappings.Select(m => m.HouseholdId).ToListAsync()).Should().Equal(householdId);
        (await db.YnabAccountCache.Select(a => a.HouseholdId).ToListAsync()).Should().Equal(householdId);
        (await db.YnabCategoryCache.Select(c => c.HouseholdId).ToListAsync()).Should().Equal(householdId);
    }

    [Fact]
    public async Task Filters_ApplyOnlyWhenScoped_AndFailClosedWithoutAHousehold()
    {
        var dbName = Guid.NewGuid().ToString();
        DbContextOptions<AppDbContext> Options() => new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options;

        Guid a, b;
        await using (var seed = new AppDbContext(Options()))
            (a, b) = await SeedTwoHouseholdsAsync(seed);

        await using (var unscoped = new AppDbContext(Options(), new FixedHousehold(false, null)))
            (await unscoped.Vehicles.CountAsync()).Should().Be(2, "startup and background work see every household");

        await using (var scopedA = new AppDbContext(Options(), new FixedHousehold(true, a)))
            await AssertSeesOnlyAsync(scopedA, a);

        await using (var noHousehold = new AppDbContext(Options(), new FixedHousehold(true, null)))
        {
            (await noHousehold.Vehicles.AnyAsync()).Should().BeFalse();
            (await noHousehold.FillUps.AnyAsync()).Should().BeFalse();
            (await noHousehold.YnabSettings.AnyAsync()).Should().BeFalse();
        }
    }

    [PostgresFact]
    public async Task Filters_TranslateOnPostgres_ForEveryScopedQueryShape()
    {
        await using var pg = await PostgresDatabase.CreateAsync();
        Guid a, b;
        Dictionary<Guid, Guid> vehicleByHousehold;
        await using (var db = pg.CreateContext())
        {
            await db.Database.MigrateAsync();
            (a, b) = await SeedTwoHouseholdsAsync(db);
            vehicleByHousehold = await db.Vehicles.ToDictionaryAsync(v => v.HouseholdId, v => v.Id);
        }

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(pg.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        foreach (var (caller, other) in new[] { (a, b), (b, a) })
        {
            await using var db = new AppDbContext(options, new FixedHousehold(true, caller));
            await AssertSeesOnlyAsync(db, caller);

            // Repository paths: listing with includes, trip miles, stats.
            var fillUps = new FillUpRepository(db);
            var (items, total) = await fillUps.ListAsync(null, null, null, 1, 25, "date", "desc");
            total.Should().Be(1);
            (await fillUps.GetTripMilesAsync(items[0])).Should().BeNull();
            (await fillUps.GetStatsAsync(null, null, null)).TotalFillUps.Should().Be(1);
            (await new VehicleRepository(db).GetByIdAsync(vehicleByHousehold[other])).Should().BeNull();
            (await new VehicleRepository(db).GetByIdAsync(vehicleByHousehold[caller])).Should().NotBeNull();

            // Location endpoint shapes: bounding box and grouped station search.
            (await db.FillUps.Where(f => f.Latitude >= 39m && f.Latitude <= 41m).CountAsync()).Should().Be(1);
            (await db.FillUps.Where(f => f.StationName.ToLower().Contains("station"))
                .GroupBy(f => f.StationName).Select(g => new { g.Key, Count = g.Count() }).ToListAsync())
                .Should().ContainSingle();

            // The memo-mapping PUT bulk-updates imports; the filter must bound it too.
            (await db.YnabImports.Where(i => i.VehicleName == "Car").ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, "touched")))
                .Should().Be(1);
        }

        await using var check = pg.CreateContext();
        (await check.YnabImports.CountAsync(i => i.Status == "touched")).Should().Be(2, "each household updated only its own import");
    }
}
