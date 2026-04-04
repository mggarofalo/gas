# Data Model

## Entity Relationships

```
vehicles 1───* fill_ups
vehicles 1───* vehicle_memo_mappings
ynab_settings (singleton)
ynab_imports (standalone queue)
ynab_account_cache (flat cache)
ynab_category_cache (flat cache)
AspNetUsers (Identity)
```

## Shared Behavior

All domain entities implement `ITimestamped`:

```csharp
public interface ITimestamped
{
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset UpdatedAt { get; set; }
}
```

`AppDbContext.SaveChanges` auto-sets `CreatedAt` on insert and `UpdatedAt` on insert/update (UTC).

All primary keys are `uuid` with `gen_random_uuid()` default. EF Core uses `UseSnakeCaseNamingConvention()`.

---

## vehicles

| Column         | Type       | Constraints                   |
| -------------- | ---------- | ----------------------------- |
| `id`           | uuid       | PK, gen_random_uuid()         |
| `year`         | smallint   | required                      |
| `make`         | varchar(100) | required                    |
| `model`        | varchar(100) | required                    |
| `notes`        | text       | nullable                      |
| `octane_rating`| smallint   | nullable, valid: 87/89/91/93  |
| `is_active`    | boolean    | default true                  |
| `created_at`   | timestamptz |                              |
| `updated_at`   | timestamptz |                              |

- Computed property (not stored): `Label = "{Year} {Make} {Model}"`
- `Label` is explicitly ignored in EF config via `builder.Ignore(v => v.Label)`
- Ordering: year asc, make asc, model asc

---

## fill_ups

| Column                    | Type          | Constraints                              |
| ------------------------- | ------------- | ---------------------------------------- |
| `id`                      | uuid          | PK, gen_random_uuid()                    |
| `vehicle_id`              | uuid          | FK -> vehicles (RESTRICT)                |
| `date`                    | date          | required                                 |
| `odometer_miles`          | integer       | required                                 |
| `gallons`                 | numeric(8,3)  | required                                 |
| `price_per_gallon`        | numeric(6,3)  | required                                 |
| `total_cost`              | numeric(8,2)  | required, computed if not provided        |
| `octane_rating`           | smallint      | nullable, inherits from vehicle if null   |
| `station_name`            | varchar(200)  | required                                 |
| `station_address`         | varchar(500)  | nullable                                 |
| `latitude`                | numeric(10,7) | nullable                                 |
| `longitude`               | numeric(10,7) | nullable                                 |
| `receipt_path`            | varchar(500)  | nullable, MinIO key: `{vehicleId}/{fillUpId}/{fileName}` |
| `notes`                   | text          | nullable                                 |
| `paperless_sync_status`   | varchar(20)   | default "none" (none/pending/synced/failed) |
| `paperless_document_id`   | integer       | nullable                                 |
| `paperless_sync_error`    | text          | nullable                                 |
| `paperless_synced_at`     | timestamptz   | nullable                                 |
| `paperless_sync_attempts` | smallint      | default 0, max 3                          |
| `ynab_sync_status`        | varchar(20)   | default "none" (none/synced/failed)       |
| `ynab_transaction_id`     | varchar(100)  | nullable, unique (filtered: NOT NULL)     |
| `ynab_sync_error`         | varchar(500)  | nullable                                 |
| `ynab_account_id`         | varchar(100)  | nullable, per-fill-up override            |
| `ynab_account_name`       | varchar(200)  | nullable                                 |
| `ynab_category_id`        | varchar(100)  | nullable, per-fill-up override            |
| `ynab_category_name`      | varchar(200)  | nullable                                 |
| `created_at`              | timestamptz   |                                           |
| `updated_at`              | timestamptz   |                                           |

### Indexes

- `ix_fill_ups_date` -- `date DESC`
- `ix_fill_ups_vehicle_date` -- `vehicle_id ASC, date DESC`
- `ix_fill_ups_ynab_transaction_id` -- unique, filtered `WHERE ynab_transaction_id IS NOT NULL`

### Computed Fields (read-time, never stored)

- **TripMiles**: `currentOdometer - previousOdometer` where previous is the same vehicle's fill-up with `date < current OR (date == current AND odometer < current)`, ordered by `date DESC, odometer DESC`
- **MPG**: `tripMiles / gallons`, rounded to 2 decimals. Nulled if `tripMiles <= 0`, `tripMiles > 600`, or `mpg > 50`
- **CostPerMile**: `totalCost / tripMiles`, rounded to 2 decimals. Only computed when MPG is plausible

### TotalCost Default

If the client omits `totalCost`, it is computed as `Math.Round(gallons * pricePerGallon, 2)`.

### OctaneRating Inheritance

On create, if `octaneRating` is null, it inherits from the vehicle's `octaneRating`.

---

## ynab_settings (singleton)

Fixed ID: `00000000-0000-0000-0000-000000000001`

| Column                  | Type         | Constraints                |
| ----------------------- | ------------ | -------------------------- |
| `id`                    | uuid         | PK, gen_random_uuid()      |
| `api_token`             | varchar(500) | required, encrypted (DPAPI) |
| `plan_id`               | varchar(100) | nullable (budget ID)       |
| `plan_name`             | varchar(200) | nullable                   |
| `account_id`            | varchar(100) | nullable (global default)  |
| `account_name`          | varchar(200) | nullable                   |
| `category_id`           | varchar(100) | nullable (global default)  |
| `category_name`         | varchar(200) | nullable                   |
| `enabled`               | boolean      | default false              |
| `last_server_knowledge` | bigint       | nullable, delta sync cursor |
| `created_at`            | timestamptz  |                            |
| `updated_at`            | timestamptz  |                            |

---

## ynab_imports

| Column               | Type         | Constraints                |
| -------------------- | ------------ | -------------------------- |
| `id`                 | uuid         | PK, gen_random_uuid()      |
| `ynab_transaction_id`| varchar(100) | required, unique           |
| `date`               | date         |                            |
| `payee_name`         | varchar(200) | required                   |
| `amount_milliunits`  | bigint       | YNAB native (negative=outflow) |
| `memo`               | varchar(500) | nullable                   |
| `gallons`            | numeric(10,3)| nullable, parsed from memo |
| `price_per_gallon`   | numeric(10,3)| nullable                   |
| `octane_rating`      | smallint     | nullable                   |
| `odometer_miles`     | integer      | nullable                   |
| `vehicle_name`       | varchar(200) | nullable, parsed from memo |
| `vehicle_id`         | uuid         | nullable, resolved via mappings |
| `status`             | varchar(20)  | default "pending" (pending/approved/dismissed) |
| `created_at`         | timestamptz  |                            |
| `updated_at`         | timestamptz  |                            |

### Indexes

- Unique on `ynab_transaction_id`
- Index on `status`

---

## ynab_account_cache

| Column       | Type         | Constraints           |
| ------------ | ------------ | --------------------- |
| `id`         | uuid         | PK, gen_random_uuid() |
| `account_id` | varchar(100) | required              |
| `name`       | varchar(200) | required              |
| `type`       | varchar(50)  | required              |
| `balance`    | bigint       | milliunits            |
| `fetched_at` | timestamptz  |                       |

Cleared and replaced on each refresh.

---

## ynab_category_cache

| Column                | Type         | Constraints           |
| --------------------- | ------------ | --------------------- |
| `id`                  | uuid         | PK, gen_random_uuid() |
| `category_id`         | varchar(100) | required, unique      |
| `name`                | varchar(200) | required              |
| `category_group_name` | varchar(200) | required              |
| `fetched_at`          | timestamptz  |                       |

---

## vehicle_memo_mappings

| Column       | Type         | Constraints                     |
| ------------ | ------------ | ------------------------------- |
| `id`         | uuid         | PK, gen_random_uuid()           |
| `memo_name`  | varchar(200) | required, unique                |
| `vehicle_id` | uuid         | FK -> vehicles (CASCADE delete) |
| `created_at` | timestamptz  |                                 |
| `updated_at` | timestamptz  |                                 |

---

## ApplicationUser (extends IdentityUser)

| Column                     | Type          | Notes                    |
| -------------------------- | ------------- | ------------------------ |
| `first_name`               | string        | nullable                 |
| `last_name`                | string        | nullable                 |
| `refresh_token`            | string        | nullable                 |
| `refresh_token_expires_at` | datetimeoffset| nullable                 |
| `must_reset_password`      | boolean       | true on seed             |
| `created_at`               | datetimeoffset|                          |
| `last_login_at`            | datetimeoffset| nullable, set on login   |

---

## Aggregate Stats Computation

- `totalMiles` = sum of `(max(odometer) - min(odometer))` per vehicle
- `avgMpg` = `totalMiles / totalGallons`
- `avgPricePerGallon` = arithmetic mean of all `price_per_gallon`
- `avgCostPerFillUp` = `totalCost / totalFillUps`
- `costPerMile` = `totalCost / totalMiles`

All stats accept optional `vehicleId`, `startDate`, `endDate` filters.
