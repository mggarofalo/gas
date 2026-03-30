# Data Model

## Entity Relationship

```
Vehicle 1───* FillUp 1───? Receipt
```

## Tables

### vehicles

| Column     | Type         | Constraints          |
| ---------- | ------------ | -------------------- |
| id         | uuid         | PK, default gen      |
| year       | smallint     | NOT NULL             |
| make       | varchar(100) | NOT NULL             |
| model      | varchar(100) | NOT NULL             |
| notes      | text         | nullable             |
| is_active  | boolean      | NOT NULL, default T  |
| created_at | timestamptz  | NOT NULL, default now|
| updated_at | timestamptz  | NOT NULL, default now|

### fill_ups

| Column          | Type           | Constraints              |
| --------------- | -------------- | ------------------------ |
| id              | uuid           | PK, default gen          |
| vehicle_id      | uuid           | FK -> vehicles, NOT NULL |
| date            | date           | NOT NULL                 |
| odometer_miles  | int            | NOT NULL                 |
| gallons         | numeric(8,3)   | NOT NULL                 |
| price_per_gallon| numeric(6,3)   | NOT NULL                 |
| total_cost      | numeric(8,2)   | NOT NULL (computed or stored) |
| station_name    | varchar(200)   | NOT NULL                 |
| station_address | varchar(500)   | nullable                 |
| latitude        | numeric(10,7)  | nullable                 |
| longitude       | numeric(10,7)  | nullable                 |
| receipt_path    | varchar(500)   | nullable (MinIO object key) |
| notes           | text           | nullable                 |
| created_at      | timestamptz    | NOT NULL, default now    |
| updated_at      | timestamptz    | NOT NULL, default now    |

### Indexes

- `ix_fill_ups_vehicle_date` on (vehicle_id, date DESC) -- primary query path
- `ix_fill_ups_date` on (date DESC) -- cross-vehicle queries

### Computed Values (Application Layer)

These are not stored; computed on read:

- **trip_miles**: current odometer - previous odometer (for same vehicle, ordered by date)
- **mpg**: trip_miles / gallons
- **cost_per_mile**: total_cost / trip_miles

## MinIO Object Layout

```
bucket: gas-receipts
key:    {vehicle_id}/{fill_up_id}/{original_filename}
```

Content-Type is preserved from the upload. Presigned GET URLs are generated on demand (1-hour expiry) for the frontend to display receipt images.
