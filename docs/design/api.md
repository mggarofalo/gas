# API Design

Base path: `/api`

All responses use JSON. Dates use ISO 8601. IDs are UUIDs. All endpoints except auth and health require a valid Bearer JWT.

---

## Auth -- `/api/auth`

| Method | Path              | Body                                        | Response                                              |
| ------ | ----------------- | ------------------------------------------- | ----------------------------------------------------- |
| POST   | `/login`          | `{email, password}`                         | `{accessToken, refreshToken, expiresIn, mustResetPassword, tokenType}` |
| POST   | `/refresh`        | `{accessToken, refreshToken}`               | Same as login                                         |
| POST   | `/logout`         | (none)                                      | 204                                                   |
| POST   | `/change-password`| `{currentPassword, newPassword}`            | Same as login (clears mustResetPassword)              |

Login and refresh are `AllowAnonymous`. Logout and change-password require auth.

---

## Vehicles -- `/api/vehicles`

| Method | Path         | Query/Body                                         | Response                  |
| ------ | ------------ | -------------------------------------------------- | ------------------------- |
| GET    | `/`          | `?active=false` to include inactive                | `VehicleDto[]`            |
| GET    | `/{id}`      |                                                    | `VehicleDto`              |
| POST   | `/`          | `CreateVehicleRequest` JSON                        | 201 + `VehicleDto`        |
| PUT    | `/{id}`      | `UpdateVehicleRequest` JSON (partial)              | `VehicleDto`              |
| DELETE | `/{id}`      |                                                    | 204 (soft-delete)         |

---

## Fill-Ups -- `/api/fill-ups`

| Method | Path                 | Body/Query                                                    | Response              |
| ------ | -------------------- | ------------------------------------------------------------- | --------------------- |
| GET    | `/`                  | `?vehicleId&startDate&endDate&page&pageSize&sortBy&sortDir`   | `FillUpPageDto`       |
| GET    | `/{id}`              |                                                               | `FillUpDto`           |
| POST   | `/`                  | `multipart/form-data` (fields + optional receipt file)        | 201 + `FillUpDto`     |
| PUT    | `/{id}`              | `multipart/form-data` (partial fields + optional receipt)     | `FillUpDto`           |
| DELETE | `/{id}`              |                                                               | 204                   |
| GET    | `/{id}/receipt`      |                                                               | 302 -> presigned URL  |
| POST   | `/{id}/resync`       |                                                               | 204 (reset Paperless) |
| POST   | `/{id}/ynab-sync`    |                                                               | `{ynabSyncStatus, ynabTransactionId, ynabSyncError}` |

### Sort options

`sortBy`: `date` (default), `odometer`, `gallons`, `total`
`sortDir`: `desc` (default), `asc`
`pageSize`: 1-100, default 25

### Receipt validation

- Max size: 10 MB
- Allowed types: `image/jpeg`, `image/png`, `image/webp`, `application/pdf`

---

## Stats -- `/api/stats`

| Method | Path     | Query                                 | Response   |
| ------ | -------- | ------------------------------------- | ---------- |
| GET    | `/`      | `?vehicleId&startDate&endDate`        | `StatsDto` |

---

## Locations

| Method | Path                   | Query        | Response                                              |
| ------ | ---------------------- | ------------ | ----------------------------------------------------- |
| GET    | `/api/locations/nearby`| `?lat&lng`   | `[{stationName, stationAddress, distanceMiles, visitCount}]` |
| GET    | `/api/stations/search` | `?q`         | `[{stationName, visitCount, lastVisit}]` (top 5)     |

Nearby uses bounding box pre-filter (0.5 miles) + Haversine exact distance. Station search requires 2+ chars, case-insensitive substring match, ordered by visit count.

---

## YNAB Settings -- `/api/settings/ynab`

| Method | Path      | Body                                                                    | Response                    |
| ------ | --------- | ----------------------------------------------------------------------- | --------------------------- |
| GET    | `/`       |                                                                         | Settings with masked token  |
| PUT    | `/token`  | `{apiToken}`                                                            | `{configured: true}`        |
| PUT    | `/`       | `{planId, planName, accountId, accountName, categoryId, categoryName, enabled}` | `{configured, enabled}` |
| DELETE | `/`       |                                                                         | 204                         |

Token is stored encrypted via Data Protection API. PUT `/` never touches the token.

---

## YNAB Proxy -- `/api/ynab`

| Method | Path                            | Response                               |
| ------ | ------------------------------- | -------------------------------------- |
| GET    | `/plans`                        | `YnabPlan[]`                           |
| GET    | `/plans/{planId}/accounts`      | `YnabAccount[]` (on-budget, open)      |
| GET    | `/plans/{planId}/categories`    | `YnabCategory[]` (non-hidden)          |
| GET    | `/accounts/cached`              | Cached accounts (auto-fetches if empty)|
| POST   | `/accounts/refresh`             | Fresh accounts from YNAB               |
| GET    | `/categories/cached`            | Cached categories (auto-fetches if empty)|
| POST   | `/categories/refresh`           | Fresh categories from YNAB             |

---

## YNAB Imports -- `/api/ynab/imports`

| Method | Path              | Body/Query                              | Response                  |
| ------ | ----------------- | --------------------------------------- | ------------------------- |
| POST   | `/pull`           | `{sinceDate?}`                          | `{newImports, skipped, errors, errorMessages}` |
| GET    | `/`               | `?status&page&pageSize`                 | `{items, page, pageSize, totalCount}` |
| PUT    | `/{id}`           | `{gallons, pricePerGallon, octaneRating, odometerMiles, vehicleId}` | `YnabImportDto` |
| POST   | `/{id}/approve`   |                                         | `{fillUpId}`              |
| POST   | `/approve-all`    |                                         | `{approved}`              |
| DELETE | `/{id}`           |                                         | 204 (marks "dismissed")   |
| POST   | `/reset`          |                                         | `{cleared}`               |

### Pull sync logic

1. Fetch transactions via delta sync (`last_server_knowledge`) or full scan
2. Skip our own pushes (import_id starts with "GAS:")
3. Skip already-imported IDs (in both `ynab_imports` and `fill_ups`)
4. Skip inflows (amount >= 0)
5. Accept if memo parseable OR category matches AND amount $5-$250
6. Auto-parse memo fields, auto-resolve vehicle from memo mappings
7. Update `last_server_knowledge` for next delta sync

### Import approval

Creates a `FillUp` with `ynab_sync_status = "synced"` and `ynab_transaction_id` set. `totalCost = abs(amountMilliunits) / 1000`.

---

## Vehicle Memo Mappings -- `/api/vehicle-memo-mappings`

| Method | Path      | Body                      | Response                      |
| ------ | --------- | ------------------------- | ----------------------------- |
| GET    | `/`       |                           | `[{id, memoName, vehicleId, vehicleLabel}]` |
| PUT    | `/`       | `{memoName, vehicleId}`   | `{mapped, importsUpdated}`    |
| DELETE | `/{id}`   |                           | 204                           |

PUT upserts the mapping and retroactively updates pending imports with matching `vehicleName`.

---

## YNAB Backfill -- `/api/admin/ynab-backfill`

| Method | Path | Body | Response |
| ------ | ---- | ---- | -------- |
| POST   | `/`  | `{sinceDate?, vehicleMappings: {name: uuid}, defaultVehicleId?, dryRun: true}` | `{total, matched, imported, skipped, failed, dryRun, errors, warnings, preview?}` |

Admin-only bulk import. Deduplicates by `(vehicleId, date, odometerMiles)`.

---

## Health -- `/health`

| Method | Path      | Response                              |
| ------ | --------- | ------------------------------------- |
| GET    | `/health` | `{status, checks: [{name, status, description, duration}]}` |

Checks PostgreSQL and MinIO connectivity.

---

## YNAB Push Sync

When a fill-up is created or manually synced:

1. Check `YnabSettings.Enabled` and plan/account configured
2. `effectiveAccountId` = per-fill-up override ?? global default
3. `amount = -(long)Math.Round(totalCost * 1000)` (YNAB milliunits, negative = outflow)
4. `memo = "{Year Make Model}, {octane|87}, ${pricePerGallon:F3}, {odometerMiles}"`
5. `importId = "GAS:{fillUp.Id:N}"[..36]` (dedup key, 36-char limit)
6. Result: status = "synced" or "failed", transaction ID stored if not duplicate

---

## MemoParser

Parses YNAB transaction memos into gas fill-up fields. Two formats:

### Push format (regex)
`{gallons}gal @ ${price}/gal, {octane} oct, {odometer}mi`

### Flexible comma format
Comma-separated fields in any order:
- Vehicle name (non-numeric text, or matches known vehicle names)
- Octane (87/89/91/93)
- Price (starts with `$` or decimal in 0.50-10.00 range)
- Odometer (integer > 1000)

Must extract at least a price to be valid. Strips wrapping `()` and `[]`.
