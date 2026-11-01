using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GasTracker.Infrastructure.Data;

/// <summary>
/// Allows <c>dotnet ef</c> to create the DbContext without booting the full app.
/// Program.cs has runtime dependencies (Data Protection key dir, MinIO, etc.)
/// that don't exist on a dev machine — this factory bypasses all of that.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        // Connection string only matters for generating migration SQL types —
        // it doesn't need to be reachable.
        optionsBuilder
            .UseNpgsql("Host=localhost;Database=gas_dev")
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }
}
