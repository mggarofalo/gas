# Backend Design

## Stack

- .NET 9 Web API (minimal APIs)
- Entity Framework Core 9 (PostgreSQL via Npgsql)
- AWS SDK for .NET (MinIO is S3-compatible)
- FluentValidation

## Project Structure

```
src/
└── GasTracker/
    ├── GasTracker.Api/              # Web API host
    │   ├── Program.cs               # Service registration, middleware, route mapping
    │   ├── Endpoints/
    │   │   ├── VehicleEndpoints.cs
    │   │   ├── FillUpEndpoints.cs
    │   │   └── StatsEndpoints.cs
    │   └── appsettings.json
    ├── GasTracker.Core/             # Domain models, interfaces
    │   ├── Entities/
    │   │   ├── Vehicle.cs
    │   │   └── FillUp.cs
    │   ├── Interfaces/
    │   │   ├── IVehicleRepository.cs
    │   │   ├── IFillUpRepository.cs
    │   │   └── IReceiptStore.cs
    │   └── DTOs/
    │       ├── FillUpDto.cs
    │       ├── VehicleDto.cs
    │       └── StatsDto.cs
    └── GasTracker.Infrastructure/   # EF Core, MinIO, implementations
        ├── Data/
        │   ├── AppDbContext.cs
        │   ├── Migrations/
        │   └── Configurations/
        │       ├── VehicleConfiguration.cs
        │       └── FillUpConfiguration.cs
        ├── Repositories/
        │   ├── VehicleRepository.cs
        │   └── FillUpRepository.cs
        └── Storage/
            └── MinioReceiptStore.cs
```

## Key Design Decisions

### Minimal APIs over Controllers

Fewer files, less ceremony. Each `*Endpoints.cs` file is a static class with an `IEndpointRouteBuilder.Map*()` extension method.

### Repository Pattern (thin)

Repositories wrap EF Core queries. They exist to keep endpoint files focused on HTTP concerns and to make the MinIO interaction testable. No generic repository abstraction -- just concrete repos per entity.

### Receipt Upload Flow

```
Client                    API                      MinIO
  │  POST multipart  ──>  │                          │
  │                        │  Validate file           │
  │                        │  Insert fill_up row      │
  │                        │  PutObject ────────────> │
  │                        │  Update receipt_path     │
  │  201 Created    <────  │                          │
```

If the MinIO upload fails after the DB insert, the fill-up is still saved without a receipt. The user can retry the receipt upload via PUT. This avoids distributed transaction complexity.

### MPG Calculation

Computed at query time, not stored. The fill-up list query uses a window function:

```sql
SELECT *,
  odometer_miles - LAG(odometer_miles) OVER (
    PARTITION BY vehicle_id ORDER BY date, odometer_miles
  ) AS trip_miles
FROM fill_ups
```

Then `mpg = trip_miles / gallons` in the mapping layer.

### Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=gastracker;Username=gas;Password=..."
  },
  "MinIO": {
    "Endpoint": "localhost:9000",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "BucketName": "gas-receipts",
    "UseSSL": false
  }
}
```

Secrets managed via user-secrets in dev, environment variables in prod.

## Middleware Pipeline

1. Exception handler (ProblemDetails)
2. CORS (allow frontend origin)
3. Static files (serve React build in prod)
4. Route endpoints
