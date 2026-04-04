# Frontend Design

## Stack

- React 19 + TypeScript
- Vite 8 (build tool)
- TailwindCSS 4
- TanStack Router (file-based, type-safe routing)
- TanStack Query 5 (server state, staleTime 30s, retry 1)
- React Hook Form + Zod 4 (via `standardSchemaResolver`)
- Recharts (dashboard charts)

## Routes

| Route                  | Component          | Purpose                                    |
| ---------------------- | ------------------ | ------------------------------------------ |
| `/login`               | LoginPage          | Email/password login                       |
| `/change-password`     | ChangePasswordPage | Forced password change on first login      |
| `/`                    | DashboardPage      | Stats cards + MPG chart + price chart + recent fill-ups |
| `/fill-ups`            | FillUpsPage        | Paginated, sortable, filterable list       |
| `/fill-ups/new`        | NewFillUpPage      | Create fill-up form                        |
| `/fill-ups/:id`        | FillUpDetailPage   | Read-only detail with sync badges          |
| `/fill-ups/:id/edit`   | EditFillUpPage     | Edit fill-up form                          |
| `/vehicles`            | VehiclesPage       | Inline edit/add/deactivate                 |
| `/settings/ynab`       | YnabSettingsPage   | Connect YNAB, select plan/account/category |
| `/settings/ynab/imports` | YnabImportsPage  | Pull queue, per-import editing, approve/dismiss |

## Auth Flow

- Tokens stored in `localStorage` under `gas_access_token` / `gas_refresh_token`
- `apiFetch` wrapper proactively refreshes on expiry and retries on 401
- Concurrent refresh calls deduplicated via a module-level promise
- `requireAuth()` guard on TanStack Router redirects to `/login` on missing token
- Also enforces `MustResetPassword` redirect to `/change-password`

## Shared Components

- **Layout**: Sidebar nav + mobile hamburger drawer
- **Spinner**: Loading indicator
- **EmptyState**: No-data placeholder
- **Toast**: Context + hook for notifications
- **CurrencyInput**: Controlled decimal input that avoids `type=number` quirks on mobile

## Dashboard

- 4 stat cards: total fill-ups, total miles, average MPG, average price per gallon
- MPG over time line chart (Recharts)
- Price per gallon over time line chart
- Recent 5 fill-ups table

## Fill-Up Form

- Vehicle selector dropdown
- Date picker
- Odometer miles input
- Gallons input
- Price per gallon input (CurrencyInput)
- Total cost input (auto-computed, manually overridable)
- Octane grade selector (87/89/91/93, defaults from vehicle)
- Station name with autocomplete (searches `/api/stations/search`)
- Station address
- GPS location (browser geolocation prompt + nearby station suggestions)
- Receipt file upload (image/PDF)
- Notes textarea
- YNAB account/category selectors (when YNAB enabled)

## Fill-Up List

- Desktop: table with sortable column headers
- Mobile: card layout
- Filters: vehicle dropdown, date range
- Pagination controls
- Each row shows: date, vehicle, station, gallons, price, total, MPG, trip miles

## YNAB Imports Page

- Pull button to trigger sync from YNAB
- Per-import inline editing (gallons, price, octane, odometer, vehicle)
- Approve single / approve all / dismiss buttons
- Vehicle memo mappings management section
- Status tabs: pending, approved, dismissed

## API Client

Single `apiFetch` function that:
1. Adds `Authorization: Bearer {token}` header
2. Checks if token is expiring soon (< 60s) and proactively refreshes
3. On 401 response: refreshes and retries once
4. On refresh failure: clears tokens and redirects to `/login`
5. Returns typed JSON response

## Zod 4 Integration

Uses `standardSchemaResolver` from `@hookform/resolvers` (not the Zod-specific resolver). This works because Zod 4 implements the Standard Schema interface.
