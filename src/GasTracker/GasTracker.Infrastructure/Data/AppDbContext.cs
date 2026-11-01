using GasTracker.Core.Entities;
using GasTracker.Core.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GasTracker.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentHousehold? currentHousehold = null)
    : IdentityDbContext<ApplicationUser>(options)
{
    // Read per query by the global filters below; EF parameterizes context members.
    private bool HouseholdScoped => currentHousehold?.IsScoped ?? false;
    private Guid? CurrentHouseholdId => currentHousehold?.HouseholdId;

    public DbSet<Household> Households => Set<Household>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<FillUp> FillUps => Set<FillUp>();
    public DbSet<YnabSettings> YnabSettings => Set<YnabSettings>();
    public DbSet<YnabAccountCache> YnabAccountCache => Set<YnabAccountCache>();
    public DbSet<YnabImport> YnabImports => Set<YnabImport>();
    public DbSet<YnabCategoryCache> YnabCategoryCache => Set<YnabCategoryCache>();
    public DbSet<VehicleMemoMapping> VehicleMemoMappings => Set<VehicleMemoMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Household isolation: inside a request every read is limited to the caller's household.
        // Users and households are not filtered; sign-in must look users up across households.
        modelBuilder.Entity<Vehicle>().HasQueryFilter(v => !HouseholdScoped || v.HouseholdId == CurrentHouseholdId);
        modelBuilder.Entity<FillUp>().HasQueryFilter(f => !HouseholdScoped || f.Vehicle.HouseholdId == CurrentHouseholdId);
        modelBuilder.Entity<YnabSettings>().HasQueryFilter(s => !HouseholdScoped || s.HouseholdId == CurrentHouseholdId);
        modelBuilder.Entity<YnabImport>().HasQueryFilter(i => !HouseholdScoped || i.HouseholdId == CurrentHouseholdId);
        modelBuilder.Entity<YnabAccountCache>().HasQueryFilter(a => !HouseholdScoped || a.HouseholdId == CurrentHouseholdId);
        modelBuilder.Entity<YnabCategoryCache>().HasQueryFilter(c => !HouseholdScoped || c.HouseholdId == CurrentHouseholdId);
        modelBuilder.Entity<VehicleMemoMapping>().HasQueryFilter(m => !HouseholdScoped || m.HouseholdId == CurrentHouseholdId);
    }

    public override int SaveChanges()
    {
        SetTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ITimestamped>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
}
