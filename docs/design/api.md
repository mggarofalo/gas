# API Design

Base path: `/api`

All responses use JSON. Dates use ISO 8601. IDs are UUIDs.

---

## Vehicles

### `GET /api/vehicles`

List all vehicles. Query param `?active=true` filters to active only (default: true).

**Response 200:**
```json
[
  {
    "id": "uuid",
    "year": 2021,
    "make": "Toyota",
    "model": "Tacoma",
    "notes": null,
    "isActive": true,
    "createdAt": "2026-01-15T00:00:00Z",
    "updatedAt": "2026-01-15T00:00:00Z"
  }
]
```

### `POST /api/vehicles`

Create a vehicle.

**Request:**
```json
{
  "year": 2021,
  "make": "Toyota",
  "model": "Tacoma",
  "notes": "Daily driver"
}
```

**Response 201:** Created vehicle object.

### `PUT /api/vehicles/{id}`

Update a vehicle. Partial updates accepted (only send changed fields).

### `DELETE /api/vehicles/{id}`

Soft-delete: sets `is_active = false`. Returns 204.

---

## Fill-Ups

### `GET /api/fill-ups`

List fill-ups with computed MPG. Supports query params:

| Param      | Type   | Default       |
| ---------- | ------ | ------------- |
| vehicleId  | uuid   | (all)         |
| startDate  | date   | (none)        |
| endDate    | date   | (none)        |
| page       | int    | 1             |
| pageSize   | int    | 25            |
| sortBy     | string | date          |
| sortDir    | string | desc          |

**Response 200:**
```json
{
  "items": [
    {
      "id": "uuid",
      "vehicleId": "uuid",
      "vehicleLabel": "2021 Toyota Tacoma",
      "date": "2026-03-28",
      "odometerMiles": 45200,
      "gallons": 14.5,
      "pricePerGallon": 3.299,
      "totalCost": 47.84,
      "stationName": "Shell",
      "stationAddress": "123 Main St",
      "latitude": 33.7490000,
      "longitude": -84.3880000,
      "receiptUrl": "https://minio.local/...",
      "tripMiles": 320,
      "mpg": 22.07,
      "costPerMile": 0.15,
      "notes": null,
      "createdAt": "2026-03-28T18:30:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "totalCount": 142
}
```

`tripMiles`, `mpg`, and `costPerMile` are null for the first fill-up per vehicle (no prior odometer reading).

### `POST /api/fill-ups`

Create a fill-up. Receipt image is uploaded via multipart form.

**Request:** `multipart/form-data`

| Field           | Type     | Required |
| --------------- | -------- | -------- |
| vehicleId       | uuid     | yes      |
| date            | date     | yes      |
| odometerMiles   | int      | yes      |
| gallons         | decimal  | yes      |
| pricePerGallon  | decimal  | yes      |
| stationName     | string   | yes      |
| stationAddress  | string   | no       |
| latitude        | decimal  | no       |
| longitude       | decimal  | no       |
| notes           | string   | no       |
| receipt         | file     | no       |

**Validation:**
- `gallons` > 0
- `pricePerGallon` > 0
- `odometerMiles` > 0
- `receipt` max size: 10 MB, allowed types: image/jpeg, image/png, image/webp, application/pdf
- `latitude` must be accompanied by `longitude` and vice versa

**Response 201:** Created fill-up object (same shape as list item).

### `PUT /api/fill-ups/{id}`

Update a fill-up. Same multipart form. Sending a new `receipt` replaces the old one in MinIO.

### `DELETE /api/fill-ups/{id}`

Hard delete. Removes MinIO object if present. Returns 204.

---

## Receipts

### `GET /api/fill-ups/{id}/receipt`

Returns a 302 redirect to a presigned MinIO URL (1-hour expiry). Returns 404 if no receipt attached.

---

## Statistics

### `GET /api/stats`

Aggregate stats. Query params: `vehicleId`, `startDate`, `endDate`.

**Response 200:**
```json
{
  "totalFillUps": 142,
  "totalGallons": 2015.5,
  "totalCost": 6650.15,
  "totalMiles": 44500,
  "avgMpg": 22.1,
  "avgPricePerGallon": 3.30,
  "avgCostPerFillUp": 46.83,
  "costPerMile": 0.15
}
```
