# Runbook: Import Historical Data from YNAB

Import past gas transactions from a YNAB CSV export into Gas Tracker using the admin ingest endpoint.

## Prerequisites

- Gas Tracker running (API accessible)
- Admin credentials (email + password)
- Vehicles already created in Gas Tracker
- YNAB account with transaction history

## Step 1: Export transactions from YNAB

1. Open YNAB and navigate to the account that has your gas transactions
2. Click the account name in the sidebar
3. Use the search/filter to narrow to gas transactions (e.g., filter by category or payee)
4. Click **Export** (top-right) and choose **CSV**
5. Save the file (e.g., `ynab-export.csv`)

The CSV will have columns like: `Account`, `Flag`, `Date`, `Payee`, `Category Group/Category`, `Category Group`, `Category`, `Memo`, `Outflow`, `Inflow`, `Cleared`.

## Step 2: Format the memo field

The ingest endpoint parses the **Memo** column using this format:

```
VehicleName, OctaneRating, $PricePerGallon, OdometerMiles
```

Examples:

```
Honda Civic, 87, $3.499, 45230
F-150, 93, $4.159, 102440
```

**If your YNAB memos don't match this format**, you need to edit them before importing. You can either:

- **Edit in YNAB** before exporting (update the memo on each gas transaction)
- **Edit the CSV** directly in a spreadsheet after exporting

Each memo must have all four parts separated by commas. The vehicle name must exactly match a key in the mappings JSON (Step 4).

## Step 3: Get your vehicle UUIDs

Log in and list vehicles:

```bash
# Get a JWT token
TOKEN=$(curl -s http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@gastracker.local","password":"YOUR_PASSWORD"}' \
  | jq -r '.accessToken')

# List vehicles
curl -s http://localhost:8080/api/vehicles \
  -H "Authorization: Bearer $TOKEN" | jq '.[] | {id, label}'
```

Note the `id` (UUID) for each vehicle.

## Step 4: Build the vehicle mappings JSON

Create a JSON object mapping the vehicle names used in your YNAB memos to their Gas Tracker UUIDs:

```json
{
  "vehicles": {
    "Honda Civic": "a1b2c3d4-...",
    "F-150": "e5f6g7h8-..."
  }
}
```

The keys must exactly match the vehicle name as it appears in the first field of each memo.

## Step 5: Dry run

Always dry-run first to validate the data without writing anything:

```bash
curl -s http://localhost:8080/api/admin/ingest \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@ynab-export.csv" \
  -F 'mappings={"vehicles":{"Honda Civic":"a1b2c3d4-...","F-150":"e5f6g7h8-..."}}' \
  -F "dryRun=true" | jq .
```

Review the response:

```json
{
  "total": 150,
  "imported": 142,
  "skipped": 0,
  "failed": 8,
  "dryRun": true,
  "errors": ["Row 12: cannot parse memo 'groceries' — expected format: vehicle, octane, $price, mileage"],
  "warnings": ["Row 45: gallons=52.3 (>50)"]
}
```

- **errors** — rows that will be skipped; fix the CSV and re-run
- **warnings** — suspicious values worth double-checking but will still import
- **skipped** — duplicates (same vehicle + date + odometer already exists)

Fix any issues in the CSV and re-run the dry run until the numbers look right.

## Step 6: Import for real

Remove the `dryRun` flag (or set it to `false`):

```bash
curl -s http://localhost:8080/api/admin/ingest \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@ynab-export.csv" \
  -F 'mappings={"vehicles":{"Honda Civic":"a1b2c3d4-...","F-150":"e5f6g7h8-..."}}' \
  | jq .
```

## Step 7: Verify

Check the fill-up count and spot-check a few entries:

```bash
# Total fill-ups
curl -s "http://localhost:8080/api/fill-ups?pageSize=1" \
  -H "Authorization: Bearer $TOKEN" | jq '.totalCount'

# Recent entries
curl -s "http://localhost:8080/api/fill-ups?sortBy=date&sortDir=asc&pageSize=5" \
  -H "Authorization: Bearer $TOKEN" | jq '.items[] | {date, stationName, totalCost, gallons}'
```

Or just open the app and browse the fill-up history page.

## How the ingest works

| Field | Source |
|-------|--------|
| Date | CSV `Date` column |
| Station | CSV `Payee` column |
| Vehicle | Memo field (1st part) mapped via `mappings` JSON |
| Octane | Memo field (2nd part) |
| Price/gal | Memo field (3rd part) |
| Odometer | Memo field (4th part) |
| Total cost | CSV `Amount`/`Outflow` column (absolute value) |
| Gallons | Calculated: `totalCost / pricePerGallon` |

Deduplication key: `(vehicleId, date, odometerMiles)`. Re-running the import is safe.

## Notes

- Imported fill-ups have no receipts, no GPS coordinates, and no notes
- Imported fill-ups will **not** sync back to YNAB (sync only triggers on new fill-ups created through the UI)
- Paperless sync status will be `none` for imported entries (no receipt to sync)
- The token from Step 3 expires; if you get a 401, re-run the login command
