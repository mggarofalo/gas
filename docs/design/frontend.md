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
- **CurrencyInput**: Controlled numeric input with an ATM-style progressive mask (see below)

### CurrencyInput and the number mask

`src/lib/numberMask.ts` holds the mask; `CurrencyInput` is the input that wears it.

- The user types digits only. They fill in from the right, so with `decimals={3}`
  the keystrokes 3, 4, 5, 9 read `0.003`, `0.034`, `0.345`, `3.459`. The decimal
  point and thousands separators are placed as you go; there is no way to type
  them, and no blur-time reformat.
- `decimals={0}` gives a whole-number field with comma grouping — that's the
  odometer.
- Backspace removes the rightmost digit; deleting them all empties the field.
- The value handed to the form is canonical (`"3.459"`, `""` when blank), never
  the display string with separators.
- `type="text"` + `inputMode="numeric"` gets iOS Safari's number pad. `type="number"`
  brings up the full keyboard there, which is what this replaced.

## Dashboard

- 4 stat cards: total fill-ups, total miles, average MPG, average price per gallon
- MPG over time line chart (Recharts)
- Price per gallon over time line chart
- Recent 5 fill-ups table

## Fill-Up Form

- Vehicle selector dropdown
- Date picker
- Odometer miles input (CurrencyInput, `decimals={0}` — comma-grouped whole miles)
- Gallons input (CurrencyInput, 3 decimals)
- Price per gallon input (CurrencyInput, 3 decimals)
- Total cost input (CurrencyInput, 2 decimals; gallons are derived from total ÷ price)
- Octane grade selector (87/89/91/93, defaults from vehicle)
- Station name with autocomplete (searches `/api/stations/search`)
- Station address
- GPS location (browser geolocation prompt + nearby station suggestions). The last
  fix is remembered in `localStorage` under `gas_last_position` for 5 minutes
  (`src/lib/geolocation.ts`), so a second request inside that window reuses it
  instead of raising another Safari permission prompt. When a remembered fix is
  used, the form offers a "Remembered — refresh" link to force a live read.
  `clearTokens()` drops the remembered fix on sign-out.
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
