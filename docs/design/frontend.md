# Frontend Design

## Stack

- React 19 + TypeScript
- Vite
- TanStack Router (file-based routing)
- TanStack Query (server state)
- Tailwind CSS 4
- React Hook Form + Zod (form validation)
- Recharts (charts)

## Routes

| Path               | View                  |
| ------------------ | --------------------- |
| `/`                | Dashboard (stats + recent fill-ups) |
| `/fill-ups`        | Fill-up history table  |
| `/fill-ups/new`    | New fill-up form       |
| `/fill-ups/:id`    | Fill-up detail / edit  |
| `/vehicles`        | Vehicle list + management |

## Pages

### Dashboard (`/`)

- Summary cards: total spend (month/year), average MPG, last fill-up
- MPG over time line chart (per vehicle)
- Cost per gallon trend line chart
- Recent fill-ups table (last 5)

### Fill-Up History (`/fill-ups`)

- Sortable, paginated table
- Columns: Date, Vehicle, Station, Gallons, Price/Gal, Total, Odometer, MPG
- Filter bar: vehicle dropdown, date range picker
- "Add Fill-Up" button

### New Fill-Up (`/fill-ups/new`)

Form fields:

1. **Vehicle** - dropdown (required), pre-selects if only one active vehicle
2. **Date** - date picker (required), defaults to today
3. **Gas Station** - text input (required)
4. **Station Address** - text input (optional)
5. **Odometer** - number input (required), label: "Current Mileage"
6. **Gallons** - number input (required), step 0.001
7. **Price per Gallon** - currency input (required), step 0.001
8. **Total Cost** - auto-computed, displayed read-only, editable to override
9. **GPS Coordinates** - two number inputs OR "Use My Location" button (browser geolocation API)
10. **Receipt** - file upload with image preview, drag-and-drop zone
11. **Notes** - textarea (optional)

Behavior:
- Total auto-computes as `gallons * pricePerGallon`, rounded to 2 decimals
- "Use My Location" calls `navigator.geolocation.getCurrentPosition()` and fills lat/lng
- Receipt preview shows thumbnail after file selection
- On submit, POSTs multipart form data, redirects to fill-up detail on success

### Fill-Up Detail (`/fill-ups/:id`)

- Read view of all fields
- Receipt image displayed (loaded via presigned URL)
- Edit button toggles to form mode (same as new, pre-populated)
- Delete with confirmation dialog

### Vehicles (`/vehicles`)

- Card or table list of vehicles
- Inline edit for year/make/model/notes
- Deactivate toggle (soft delete)
- Add vehicle modal/inline form

## Component Hierarchy

```
App
├── Layout (nav sidebar or top bar)
│   ├── DashboardPage
│   │   ├── StatCard (x4)
│   │   ├── MpgChart
│   │   ├── PriceChart
│   │   └── RecentFillUpsTable
│   ├── FillUpListPage
│   │   ├── FilterBar
│   │   └── FillUpTable (paginated)
│   ├── FillUpFormPage
│   │   ├── VehicleSelect
│   │   ├── DatePicker
│   │   ├── GpsInput
│   │   ├── ReceiptUpload
│   │   └── FormActions
│   ├── FillUpDetailPage
│   │   └── ReceiptViewer
│   └── VehiclesPage
│       ├── VehicleCard
│       └── VehicleForm
```

## Responsive Behavior

Mobile-first layout. The fill-up form is the primary mobile use case (logging at the pump).

- Dashboard: cards stack vertically, charts full-width
- Fill-up table: horizontal scroll on narrow screens, or collapses to card view
- Form: single-column, large touch targets
