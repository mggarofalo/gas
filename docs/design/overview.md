# Gas Tracker - Design Overview

## Purpose

A personal fuel tracking application for logging fill-ups, storing receipt images, and tracking fuel economy across vehicles.

## Tech Stack

| Layer        | Technology                     |
| ------------ | ------------------------------ |
| Frontend     | React (TypeScript, Vite)       |
| Backend      | .NET 9 Web API (C#)            |
| Database     | PostgreSQL                     |
| Object Store | MinIO (S3-compatible)          |
| Hosting      | Docker Compose (self-hosted)   |

## High-Level Architecture

```
┌─────────────┐       ┌──────────────────┐       ┌────────────┐
│   React SPA │──────>│  .NET Web API    │──────>│ PostgreSQL │
│  (Vite/TS)  │<──────│  (REST + JSON)   │<──────│            │
└─────────────┘       └────────┬─────────┘       └────────────┘
                               │
                               v
                        ┌────────────┐
                        │   MinIO    │
                        │ (receipts) │
                        └────────────┘
```

## Core Workflows

### 1. Log a Fill-Up

User selects a vehicle, enters date, mileage, gallons, price per gallon, gas station name, and optionally attaches a receipt photo and GPS coordinates. The API persists the record and uploads the receipt image to MinIO.

### 2. Manage Vehicles

User creates/edits a small set of preconfigured vehicles (year, make, model, notes). Vehicles are referenced by fill-up entries.

### 3. View History

User browses fill-up history with computed MPG, cost per mile, and totals. Filterable by vehicle and date range.

## Non-Goals (v1)

- Multi-user / authentication (single-user app)
- OCR / automatic receipt parsing
- Mobile-native app (responsive web only)
- Real-time GPS capture (manual entry or browser geolocation prompt)
