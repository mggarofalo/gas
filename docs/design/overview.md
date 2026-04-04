# Gas Tracker - Design Overview

## Purpose

A self-hosted, single-household fuel tracking application. Records every gas fill-up with full transactional data (cost, gallons, price per gallon, odometer) and derives fuel economy and expense metrics. Integrates bidirectionally with YNAB for budget sync and with Paperless-ngx for receipt archival.

## Tech Stack

| Layer         | Technology                                          |
| ------------- | --------------------------------------------------- |
| Frontend      | React 19, TypeScript, Vite, TailwindCSS, TanStack   |
| Backend       | .NET 10 (ASP.NET Core Minimal APIs, C#)             |
| Database      | PostgreSQL 17 via EF Core + Npgsql                  |
| Object Store  | MinIO (S3-compatible, via AWS SDK)                   |
| Auth          | JWT (HS256) + refresh tokens, ASP.NET Core Identity |
| Doc Sync      | Paperless-ngx (async background service)            |
| Budget Sync   | YNAB API (bidirectional push/pull)                   |
| Hosting       | Docker Compose (self-hosted, multi-arch amd64/arm64) |

## High-Level Architecture

```
                         ┌──────────────┐
                         │  YNAB API    │
                         └──────┬───────┘
                                │ push/pull
┌─────────────┐       ┌────────┴─────────┐       ┌────────────┐
│   React SPA │──────>│  .NET Web API    │──────>│ PostgreSQL │
│  (Vite/TS)  │<──────│  (REST + JSON)   │<──────│            │
└─────────────┘       └──┬──────────┬────┘       └────────────┘
                         │          │
                         v          v
                  ┌──────────┐  ┌────────────────┐
                  │  MinIO   │  │ Paperless-ngx  │
                  │(receipts)│  │ (doc archive)  │
                  └──────────┘  └────────────────┘
```

The React SPA is served from `wwwroot` by ASP.NET static files middleware, with `MapFallbackToFile("index.html")` for SPA routing. All API routes are under `/api/`.

## Core Workflows

### 1. Log a Fill-Up

User selects a vehicle, enters date, mileage, gallons, price per gallon, station name, and optionally attaches a receipt photo, GPS coordinates, octane grade, and YNAB account/category overrides. The API persists the record, uploads the receipt to MinIO, sets Paperless sync to "pending", and pushes a transaction to YNAB (inline, fire-and-forget on failure).

### 2. Manage Vehicles

Create/edit vehicles (year, make, model, notes, preferred octane). Soft-delete via `is_active = false`. Vehicles are referenced by fill-ups with `ON DELETE RESTRICT`.

### 3. View Dashboard

Stat cards (total fill-ups, total miles, avg MPG, avg cost) and two time-series charts (MPG over time, price per gallon over time). Recent 5 fill-ups shown below.

### 4. Browse Fill-Up History

Paginated, sortable, filterable list. Desktop table with mobile card layout. Each fill-up shows computed trip miles, MPG, and cost per mile. Sync status badges for Paperless and YNAB with retry actions.

### 5. YNAB Integration

- **Push**: fill-ups automatically sync to YNAB as transactions on create
- **Pull**: YNAB transactions pulled into a review queue via delta sync
- **Import Review**: edit parsed fields, approve into fill-ups, dismiss junk
- **Backfill**: admin bulk import of historical YNAB transactions
- **Memo Parsing**: extracts vehicle name, octane, price, odometer from transaction memos
- **Vehicle Memo Mappings**: maps memo strings to vehicle IDs for auto-resolution

### 6. Receipt Archival

Receipts uploaded to MinIO. Background service polls for pending receipts and syncs to Paperless-ngx with exponential backoff (max 3 attempts).

### 7. Authentication

JWT with refresh tokens. Admin user seeded on first launch with `MustResetPassword = true`. Forced password change on first login.

## Project Structure

```
src/
  GasTracker/
    GasTracker.Core/          # Domain entities, interfaces, DTOs, MemoParser
    GasTracker.Infrastructure/ # EF Core, repositories, auth, storage, external clients
    GasTracker.Api/           # Endpoints, validators, mappings, Program.cs
    GasTracker.Tests/         # xUnit + FluentAssertions, EF InMemory
    GasTracker.slnx           # Solution file
  frontend/                   # React SPA (Vite + TanStack)
docs/design/                  # These design documents
Dockerfile                    # Multi-stage: Node build + .NET build + runtime
docker-compose.yml            # init, db, minio, app services
docker-entrypoint.sh          # Secrets bridge + app launch
```

## Design Principles

- **Three-layer split**: Core (domain, no dependencies), Infrastructure (persistence, external), Api (HTTP)
- **Computed at read time**: MPG, trip miles, cost per mile are never stored — computed in the mapping layer
- **Plausibility guards**: trip > 600mi or MPG > 50 nulls out fuel economy (preserves record for price history)
- **Soft delete**: Vehicles are deactivated, never hard-deleted (fill-ups reference them)
- **Encrypted at rest**: YNAB API token encrypted via Data Protection API
- **Auto-migrate**: `db.Database.Migrate()` runs on startup
- **Central package management**: `Directory.Packages.props` pins all NuGet versions
