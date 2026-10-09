# Backend Design

## Stack

- .NET 10 Web API (minimal APIs)
- EF Core 10 + Npgsql + snake_case naming convention
- ASP.NET Core Identity for user management
- JWT (HS256) authentication with refresh tokens
- FluentValidation for request validation
- Data Protection API for encrypting YNAB tokens at rest
- AWS SDK for S3 (S3Mock client)
- BackgroundService for async Paperless sync

## Project Structure

### GasTracker.Core (no external dependencies except Identity stores)

```
Entities/
  Vehicle.cs          # Year, Make, Model, Notes, OctaneRating, IsActive, Label (computed)
  FillUp.cs           # All fill-up fields + Paperless/YNAB sync state
  YnabSettings.cs     # Singleton config (encrypted token, plan/account/category, enabled)
  YnabImport.cs       # Import review queue entry
  YnabAccountCache.cs # Flat cache row
  YnabCategoryCache.cs
  VehicleMemoMapping.cs
  ApplicationUser.cs  # Extends IdentityUser with refresh token, MustResetPassword
Interfaces/
  ITimestamped.cs     # CreatedAt, UpdatedAt
  IVehicleRepository.cs
  IFillUpRepository.cs
  IReceiptStore.cs
  ITokenService.cs
  IPaperlessClient.cs
  IYnabClient.cs      # + record types for YNAB DTOs
DTOs/
  VehicleDto.cs       # + CreateVehicleRequest, UpdateVehicleRequest
  FillUpDto.cs        # + FillUpPageDto, CreateFillUpRequest, StatsDto
MemoParser.cs         # Static parser with push-format regex + flexible comma parser
```

### GasTracker.Infrastructure

```
Data/
  AppDbContext.cs              # IdentityDbContext<ApplicationUser>, auto-timestamps
  Configurations/
    VehicleConfiguration.cs
    FillUpConfiguration.cs
    YnabSettingsConfiguration.cs
    YnabImportConfiguration.cs
    YnabAccountCacheConfiguration.cs
    YnabCategoryCacheConfiguration.cs
    VehicleMemoMappingConfiguration.cs
  Migrations/                  # EF Core auto-generated
Repositories/
  VehicleRepository.cs         # CRUD + active filter + ordering
  FillUpRepository.cs          # CRUD + filtering + sorting + pagination + TripMiles + Stats
Auth/
  JwtOptions.cs                # Key, Issuer, Audience, ExpiryMinutes/Days
  TokenService.cs              # JWT generation, refresh token storage, expired token validation
  AdminSeeder.cs               # Seeds admin user on first launch
Storage/
  S3Options.cs              # Endpoint, AccessKey, SecretKey, BucketName, UseSSL
  S3ReceiptStore.cs         # Upload, download, delete, presigned URL
  S3HealthCheck.cs          # Health check for S3Mock connectivity
Paperless/
  PaperlessOptions.cs          # BaseUrl, Token, Enabled, PollIntervalSeconds
  PaperlessClient.cs           # Upload document, ensure tags, health check
  NoOpPaperlessClient.cs       # Stub when Paperless disabled
  PaperlessSyncService.cs      # BackgroundService with exponential backoff
Ynab/
  YnabClient.cs                # HTTP client for YNAB API (plans, accounts, categories, transactions)
  YnabTokenService.cs          # Decrypts stored YNAB API token via DPAPI
  YnabPullSyncService.cs       # Pull transactions into import queue with dedup + memo parsing
```

### GasTracker.Api

```
Endpoints/
  AuthEndpoints.cs             # Login, refresh, logout, change-password
  VehicleEndpoints.cs          # CRUD with soft-delete
  FillUpEndpoints.cs           # CRUD + receipt + YNAB push sync
  StatsEndpoints.cs            # Aggregate stats
  LocationEndpoints.cs         # Nearby stations (Haversine) + station search
  YnabSettingsEndpoints.cs     # Token, settings, delete
  YnabProxyEndpoints.cs        # Pass-through to YNAB API + caching
  YnabImportEndpoints.cs       # Pull, list, update, approve, dismiss, reset, memo mappings
  YnabBackfillEndpoints.cs     # Admin bulk import
Validators/
  FillUpValidators.cs          # CreateFillUpValidator
  VehicleValidators.cs         # CreateVehicleValidator, UpdateVehicleValidator
Mappings.cs                    # Vehicle.ToDto(), FillUp.ToDto(tripMiles) with plausibility guards
Program.cs                     # DI, middleware, endpoint registration
```

## Key Business Rules

### TripMiles Computation

```sql
SELECT TOP 1 odometer_miles
FROM fill_ups
WHERE vehicle_id = @vehicleId AND id != @id
  AND (date < @date OR (date = @date AND odometer_miles < @odometer))
ORDER BY date DESC, odometer_miles DESC
```

`tripMiles = current.OdometerMiles - previous.OdometerMiles`

### MPG Plausibility Guards

```csharp
var plausibleTrip = tripMiles > 0 && tripMiles <= 600;
var rawMpg = plausibleTrip && gallons > 0
    ? Math.Round(tripMiles / gallons, 2) : null;
var mpg = rawMpg is > 0 and <= 50 ? rawMpg : null;
var costPerMile = plausibleTrip && mpg.HasValue
    ? Math.Round(totalCost / tripMiles, 2) : null;
```

### YNAB Amount Convention

`amount = -(long)Math.Round(totalCost * 1000)` (milliunits, negative = outflow)

### YNAB Bank Matching and Push Retries

New fill-ups are sent as approved, uncleared, user-entered transactions: `import_id`
is omitted. Supplying it marks the API transaction as imported and prevents matching
with a later bank import. See [YNAB's API FAQ](https://support.ynab.com/en_us/the-ynab-api-an-overview-BJMgQ3zAq).

`YnabPushSyncService` appends `[GAS:<fill-up GUID in N format>]` to the memo for
recovery without changing the import classification. Before sending, it looks for
that reference (or the legacy `GAS:` import ID) across the configured plan. Linked
or previously synced fill-ups are not recreated. Pull sync and backfill skip both
forms of GAS reference; memo parsing ignores the new marker.

The service saves `pending` before posting and uses the sync status as an EF
concurrency token to prevent concurrent sends. Explicit API rejections permit a
retry. Timeouts, server errors, missing transaction IDs, or an interrupted process
leave the outcome uncertain: subsequent calls to `POST /api/fill-ups/{id}/ynab-sync`
only look for the transaction and never blindly resend it. The response includes
`ynabSyncError` explaining unresolved outcomes. If the transaction remains missing,
manual reconciliation is required; verify the original plan/account before any new
entry. Keep the memo reference intact until recovery completes. This change does
not repair or recreate previously imported transactions and needs no schema migration.

### Paperless Sync Backoff

Attempt N: wait `2^N * 30` seconds. After 3 failures: permanently "failed".

### Location Search

- Bounding box: +/- 0.5 miles converted to degrees (lat: 69.0 mi/deg, lng: 54.6 mi/deg at ~38 latitude)
- Haversine formula with R = 3958.8 miles
- Group by (stationName, stationAddress), return closest distance + visit count

## Configuration

| Key | Purpose | Source |
| --- | ------- | ------ |
| `ConnectionStrings:Default` | PostgreSQL connection string | Secret file -> env |
| `Jwt:Key` | HMAC-SHA256 signing key (32+ bytes) | Secret file -> env |
| `Jwt:Issuer` / `Jwt:Audience` | JWT claims | Env (default: gas-api/gas-app) |
| `Jwt:AccessTokenExpiryMinutes` | Access token TTL | Config (default: 60) |
| `Jwt:RefreshTokenExpiryDays` | Refresh token TTL | Config (default: 30) |
| `S3:Endpoint` | S3Mock host:port | Env |
| `S3:AccessKey` / `S3:SecretKey` | S3Mock credentials | Secret file -> env |
| `S3:BucketName` | S3 bucket | Env (default: gas-receipts) |
| `S3:UseSSL` | TLS to S3Mock | Env (default: false) |
| `Paperless:Enabled` | Enable Paperless sync | Env |
| `Paperless:BaseUrl` | Paperless-ngx URL | Env |
| `Paperless:Token` | Paperless API token | Env |
| `Paperless:PollIntervalSeconds` | Sync poll rate | Config (default: 30) |
| `AdminSeed:Email` | Admin user email | Env |
| `AdminSeed:Password` | Admin initial password | Secret file -> env |
| `AllowedOrigins` | CORS origins | Env (default: http://localhost:5173) |

## Identity Configuration

- Password: 12+ chars, requires digit, uppercase, lowercase, non-alphanumeric
- Admin user seeded with `MustResetPassword = true`
- `EmailConfirmed = true` (no email verification flow)

## Health Checks

- PostgreSQL: `AddDbContextCheck<AppDbContext>("postgresql")`
- S3: custom `S3HealthCheck` that calls `GetBucketLocationAsync`
