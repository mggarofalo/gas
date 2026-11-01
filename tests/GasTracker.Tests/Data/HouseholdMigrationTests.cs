using FluentAssertions;
using GasTracker.Core.Entities;
using GasTracker.Infrastructure.Auth;
using GasTracker.Infrastructure.Data;
using GasTracker.Infrastructure.Data.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GasTracker.Tests.Data;

/// <summary>
/// Runs the real migrations against PostgreSQL. Set POSTGRES_TEST_CONNECTION to a server
/// connection string (no database); each test creates and drops its own database.
/// </summary>
public class HouseholdMigrationTests
{
    // Last migration before households existed: the schema production runs today.
    private const string PreHouseholdMigration = "20260403111007_AddVehicleMemoMappings";
    private static readonly Guid DefaultHouseholdId = Guid.Parse(AddHouseholds.DefaultHouseholdId);

    private static readonly string[] HouseholdTables =
    [
        "AspNetUsers", "vehicles", "vehicle_memo_mappings", "ynab_settings",
        "ynab_imports", "ynab_account_cache", "ynab_category_cache",
    ];

    [Fact]
    public void Model_HasNoChangesMissingFromMigrations()
    {
        using var db = new DesignTimeDbContextFactory().CreateDbContext([]);

        db.Database.HasPendingModelChanges().Should().BeFalse(
            "every model change must ship with a migration (run dotnet ef migrations add)");
    }

    [PostgresFact]
    public async Task FreshDatabase_MigratesAndBootstrapsOneHouseholdWithItsOwner()
    {
        await using var pg = await PostgresDatabase.CreateAsync();
        await using (var db = pg.CreateContext())
            await db.Database.MigrateAsync();

        await BootstrapAsync(pg);
        await BootstrapAsync(pg); // Second start-up is a no-op.

        await using var check = pg.CreateContext();
        var household = await check.Households.Include(h => h.Members).SingleAsync();
        household.Name.Should().Be(HouseholdBootstrapper.DefaultHouseholdName);
        var owner = household.Members.Should().ContainSingle().Subject;
        owner.Email.Should().Be("owner@example.com");
        owner.HouseholdRole.Should().Be(HouseholdRole.Owner);
    }

    [PostgresFact]
    public async Task ExistingDatabase_BackfillsEveryRowIntoTheDefaultHousehold_WithZeroDataLoss()
    {
        await using var pg = await PostgresDatabase.CreateAsync();
        await using (var db = pg.CreateContext())
            await db.GetService<IMigrator>().MigrateAsync(PreHouseholdMigration);
        await pg.ExecuteAsync(LegacySeed.Sql(fillUpsPerVehicle: 150));
        var before = await pg.FingerprintAsync();
        before["fill_ups"].Rows.Should().Be(450);

        await using (var db = pg.CreateContext())
            await db.Database.MigrateAsync();

        (await pg.FingerprintAsync(before)).Should().BeEquivalentTo(before, "no pre-existing row or column may change");
        await using (var db = pg.CreateContext())
        {
            var household = await db.Households.SingleAsync();
            household.Id.Should().Be(DefaultHouseholdId);
            household.Name.Should().Be("Default");
            var admin = await db.Users.SingleAsync();
            admin.HouseholdId.Should().Be(DefaultHouseholdId);
            admin.HouseholdRole.Should().Be(HouseholdRole.Owner);
            (await db.Vehicles.Include(v => v.Household).CountAsync(v => v.Household.Id == DefaultHouseholdId)).Should().Be(3);
            (await db.FillUps.CountAsync(f => f.Vehicle.HouseholdId == DefaultHouseholdId)).Should().Be(450);
        }
        foreach (var table in HouseholdTables)
        {
            var total = await pg.ScalarAsync<long>($"""SELECT count(*) FROM "{table}" """);
            var backfilled = await pg.ScalarAsync<long>($"""SELECT count(*) FROM "{table}" WHERE household_id = '{DefaultHouseholdId}'""");
            backfilled.Should().Be(total, $"every {table} row belongs to the default household").And.BePositive();
        }

        await BootstrapAsync(pg); // The admin already exists, so no second household appears.
        (await pg.ScalarAsync<long>("SELECT count(*) FROM households")).Should().Be(1);

        // Backward: rolling back restores the exact pre-household data.
        await using (var db = pg.CreateContext())
            await db.GetService<IMigrator>().MigrateAsync(PreHouseholdMigration);
        (await pg.FingerprintAsync(before)).Should().BeEquivalentTo(before);
        (await pg.ScalarAsync<long>("SELECT count(*) FROM information_schema.columns WHERE column_name IN ('household_id', 'household_role')")).Should().Be(0);

        // And forward again.
        await using (var db = pg.CreateContext())
            await db.Database.MigrateAsync();
        (await pg.FingerprintAsync(before)).Should().BeEquivalentTo(before);
        (await pg.ScalarAsync<long>("SELECT count(*) FROM households")).Should().Be(1);
    }

    [PostgresFact]
    public async Task ExistingDataWithoutUsers_BootstrapOwnerJoinsTheBackfilledHousehold()
    {
        await using var pg = await PostgresDatabase.CreateAsync();
        await using (var db = pg.CreateContext())
            await db.GetService<IMigrator>().MigrateAsync(PreHouseholdMigration);
        await pg.ExecuteAsync("""
            INSERT INTO vehicles (year, make, model, created_at, updated_at)
            VALUES (2018, 'Ford', 'F-150', now(), now());
            """);
        await using (var db = pg.CreateContext())
            await db.Database.MigrateAsync();

        await BootstrapAsync(pg);

        await using var check = pg.CreateContext();
        (await check.Households.SingleAsync()).Id.Should().Be(DefaultHouseholdId);
        (await check.Users.SingleAsync()).HouseholdId.Should().Be(DefaultHouseholdId);
    }

    [PostgresFact]
    public async Task EmptyDatabase_MigrationCreatesNoHousehold()
    {
        await using var pg = await PostgresDatabase.CreateAsync();
        await using var db = pg.CreateContext();

        await db.Database.MigrateAsync();

        (await db.Households.CountAsync()).Should().Be(0);
    }

    private static async Task BootstrapAsync(PostgresDatabase pg)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AdminSeed:Email"] = "owner@example.com",
            ["AdminSeed:Password"] = "StrongPassword123!!",
        }).Build());
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(pg.ConnectionString).UseSnakeCaseNamingConvention());
        services.AddIdentityCore<ApplicationUser>().AddEntityFrameworkStores<AppDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await HouseholdBootstrapper.BootstrapAsync(scope.ServiceProvider);
    }
}

public sealed record TableFingerprint(long Rows, string Hash);

/// <summary>A uniquely named database on the server named by POSTGRES_TEST_CONNECTION.</summary>
public sealed class PostgresDatabase : IAsyncDisposable
{
    private readonly string _serverConnectionString;
    private readonly string _name;

    private PostgresDatabase(string serverConnectionString, string name)
    {
        _serverConnectionString = serverConnectionString;
        _name = name;
        ConnectionString = new NpgsqlConnectionStringBuilder(serverConnectionString) { Database = name, Pooling = false }.ConnectionString;
    }

    public string ConnectionString { get; }

    public static async Task<PostgresDatabase> CreateAsync()
    {
        var server = Environment.GetEnvironmentVariable(PostgresFactAttribute.Variable)!;
        var db = new PostgresDatabase(server, $"gas_test_{Guid.NewGuid():N}");
        await db.ExecuteOnServerAsync($"CREATE DATABASE {db._name}");
        return db;
    }

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(ConnectionString)
        .UseSnakeCaseNamingConvention()
        .Options);

    public async Task ExecuteAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<T> ScalarAsync<T>(string sql)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    /// <summary>
    /// Row count and content hash per table. With a <paramref name="baseline"/> from an earlier call,
    /// fingerprints only that baseline's tables and columns, so added columns don't count as changes.
    /// </summary>
    public async Task<Dictionary<string, TableFingerprint>> FingerprintAsync(Dictionary<string, TableFingerprint>? baseline = null)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        var columns = new Dictionary<string, List<string>>();
        await using (var cmd = new NpgsqlCommand("""
            SELECT table_name, column_name FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
            ORDER BY table_name, ordinal_position
            """, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                var table = reader.GetString(0);
                if (!columns.TryGetValue(table, out var list)) columns[table] = list = [];
                list.Add(reader.GetString(1));
            }
        }

        var result = new Dictionary<string, TableFingerprint>();
        foreach (var (table, tableColumns) in columns)
        {
            if (baseline is not null && !baseline.ContainsKey(table)) continue;
            var cols = baseline is null ? tableColumns : BaselineColumns![table];
            var row = string.Join(", ", cols.Select(c => $"\"{c}\""));
            await using var cmd = new NpgsqlCommand($"""
                SELECT count(*), coalesce(md5(string_agg(r, E'\n' ORDER BY r)), '')
                FROM (SELECT ROW({row})::text AS r FROM "{table}") s
                """, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            result[table] = new TableFingerprint(reader.GetInt64(0), reader.GetString(1));
        }

        BaselineColumns ??= columns;
        return result;
    }

    // Columns seen by the first fingerprint, reused by later comparisons.
    private Dictionary<string, List<string>>? BaselineColumns { get; set; }

    public async ValueTask DisposeAsync() => await ExecuteOnServerAsync($"DROP DATABASE IF EXISTS {_name} WITH (FORCE)");

    private async Task ExecuteOnServerAsync(string sql)
    {
        await using var conn = new NpgsqlConnection(_serverConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }
}

public sealed class PostgresFactAttribute : FactAttribute
{
    public const string Variable = "POSTGRES_TEST_CONNECTION";

    public PostgresFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Variable)))
            Skip = $"Set {Variable} to a PostgreSQL server connection string to run migration tests.";
    }
}

/// <summary>
/// Data shaped like a long-running single-user install, written against the pre-household schema.
/// </summary>
public static class LegacySeed
{
    public static string Sql(int fillUpsPerVehicle) => $"""
        INSERT INTO "AspNetUsers" (id, first_name, last_name, refresh_token, refresh_token_expires_at,
            must_reset_password, created_at, last_login_at, user_name, normalized_user_name, email,
            normalized_email, email_confirmed, password_hash, security_stamp, concurrency_stamp,
            phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count)
        VALUES ('9b2e7c1a-5f3d-4e8b-a6c4-1d2e3f4a5b6c', 'Admin', 'User', 'refresh-token-value',
            '2026-11-30 12:00:00+00', false, '2026-03-31 01:26:02+00', '2026-10-30 18:45:10+00',
            'admin@gastracker.local', 'ADMIN@GASTRACKER.LOCAL', 'admin@gastracker.local',
            'ADMIN@GASTRACKER.LOCAL', true, 'AQAAAAIAAYagAAAAEExampleHashValue==', 'SECURITYSTAMP',
            'c0ffee00-0000-4000-8000-000000000000', false, false, true, 0);

        INSERT INTO vehicles (id, year, make, model, notes, is_active, created_at, updated_at, octane_rating) VALUES
            ('a1000000-0000-4000-8000-000000000001', 2022, 'Toyota', 'Sienna', 'Family van — 8 seats', true, '2026-03-31 02:00:00+00', '2026-06-01 10:00:00+00', 87),
            ('a1000000-0000-4000-8000-000000000002', 2015, 'Honda', 'Civic', NULL, false, '2026-03-31 02:05:00+00', '2026-09-15 09:30:00+00', NULL),
            ('a1000000-0000-4000-8000-000000000003', 2019, 'Subaru', 'Outback', 'Winter tires Nov–Mar', true, '2026-04-02 14:00:00+00', '2026-04-02 14:00:00+00', 91);

        INSERT INTO fill_ups (vehicle_id, date, odometer_miles, gallons, price_per_gallon, total_cost,
            station_name, station_address, latitude, longitude, receipt_path, notes,
            paperless_sync_status, paperless_document_id, paperless_sync_error, paperless_synced_at,
            paperless_sync_attempts, created_at, updated_at, octane_rating, ynab_sync_error,
            ynab_sync_status, ynab_transaction_id, ynab_account_id, ynab_account_name,
            ynab_category_id, ynab_category_name)
        SELECT v.id,
            date '2026-10-30' - g * 4,
            40000 - g * 290,
            (8 + g % 9)::numeric + 0.237,
            3.099 + (g % 13) * 0.05,
            round(((8 + g % 9) + 0.237) * (3.099 + (g % 13) * 0.05), 2),
            CASE g % 4 WHEN 0 THEN 'Shell' WHEN 1 THEN 'Wawa' WHEN 2 THEN 'Sheetz' ELSE 'Café Fuel & Go' END,
            CASE WHEN g % 3 = 0 THEN NULL ELSE g || ' Main St, Springfield' END,
            CASE WHEN g % 5 = 0 THEN NULL ELSE 40.1234567 + g * 0.0001 END,
            CASE WHEN g % 5 = 0 THEN NULL ELSE -75.7654321 - g * 0.0001 END,
            CASE WHEN g % 2 = 0 THEN v.id || '/' || gen_random_uuid() || '/receipt.jpg' END,
            CASE WHEN g % 10 = 0 THEN 'Topped off; "premium" by mistake' END,
            CASE g % 3 WHEN 0 THEN 'synced' WHEN 1 THEN 'none' ELSE 'failed' END,
            CASE WHEN g % 3 = 0 THEN 1000 + g END,
            CASE WHEN g % 3 = 2 THEN 'Paperless returned 502' END,
            CASE WHEN g % 3 = 0 THEN timestamptz '2026-10-30 00:00:00+00' - g * interval '4 days' END,
            (g % 3)::smallint,
            timestamptz '2026-10-30 12:00:00+00' - g * interval '4 days',
            timestamptz '2026-10-30 12:00:00+00' - g * interval '4 days' + interval '5 minutes',
            CASE WHEN g % 7 = 0 THEN 93 ELSE 87 END,
            CASE WHEN g % 11 = 0 THEN 'YNAB rate limited' END,
            CASE WHEN g % 2 = 0 THEN 'synced' WHEN g % 11 = 0 THEN 'failed' ELSE 'none' END,
            CASE WHEN g % 2 = 0 THEN 'ynab-' || v.id || '-' || g END,
            CASE WHEN g % 2 = 0 THEN 'acct-checking' END,
            CASE WHEN g % 2 = 0 THEN 'Checking' END,
            CASE WHEN g % 2 = 0 THEN 'cat-gas' END,
            CASE WHEN g % 2 = 0 THEN 'Gas & Fuel' END
        FROM vehicles v CROSS JOIN generate_series(1, {fillUpsPerVehicle}) g;

        INSERT INTO ynab_settings (id, api_token, plan_id, plan_name, account_id, account_name,
            category_id, category_name, enabled, created_at, updated_at, last_server_knowledge)
        VALUES ('00000000-0000-0000-0000-000000000001', 'CfDJ8EncryptedTokenPayload', 'plan-1', 'Household Budget',
            'acct-checking', 'Checking', 'cat-gas', 'Gas & Fuel', true, '2026-04-01 07:40:00+00', '2026-10-30 18:00:00+00', 98765);

        INSERT INTO ynab_account_cache (account_id, name, type, balance, fetched_at)
        SELECT 'acct-' || g, 'Account ' || g, CASE WHEN g % 2 = 0 THEN 'checking' ELSE 'creditCard' END, g * -123450, '2026-10-30 18:00:00+00'
        FROM generate_series(1, 4) g;

        INSERT INTO ynab_category_cache (category_id, name, category_group_name, fetched_at)
        SELECT 'cat-' || g, 'Category ' || g, 'Group ' || (g % 3), '2026-10-30 18:00:00+00'
        FROM generate_series(1, 12) g;

        INSERT INTO ynab_imports (ynab_transaction_id, date, payee_name, amount_milliunits, memo, gallons,
            price_per_gallon, octane_rating, odometer_miles, vehicle_name, vehicle_id, status, created_at, updated_at)
        SELECT 'bank-' || g, date '2026-10-01' + g, 'Payee ' || g, -30000 - g * 100,
            CASE WHEN g % 2 = 0 THEN 'Sienna 87 3.299 ' || (12000 + g) END,
            CASE WHEN g % 2 = 0 THEN 10.5 END, CASE WHEN g % 2 = 0 THEN 3.299 END,
            CASE WHEN g % 2 = 0 THEN 87 END, CASE WHEN g % 2 = 0 THEN 12000 + g END,
            CASE WHEN g % 2 = 0 THEN 'Sienna' END,
            CASE WHEN g % 4 = 0 THEN 'a1000000-0000-4000-8000-000000000001'::uuid END,
            CASE g % 3 WHEN 0 THEN 'pending' WHEN 1 THEN 'approved' ELSE 'dismissed' END,
            '2026-10-02 00:00:00+00', '2026-10-02 00:00:00+00'
        FROM generate_series(1, 25) g;

        INSERT INTO vehicle_memo_mappings (memo_name, vehicle_id, created_at, updated_at) VALUES
            ('Sienna', 'a1000000-0000-4000-8000-000000000001', '2026-04-03 11:10:00+00', '2026-04-03 11:10:00+00'),
            ('Outback', 'a1000000-0000-4000-8000-000000000003', '2026-04-03 11:12:00+00', '2026-04-03 11:12:00+00');
        """;
}
