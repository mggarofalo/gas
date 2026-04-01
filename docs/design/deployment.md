# Deployment

## Architecture

Single `docker-compose.yml` runs the full stack. The API and React SPA are combined into one container — .NET serves the SPA from `wwwroot` alongside the API endpoints. No nginx reverse proxy is needed.

### Services

| Service | Image | Purpose |
| ------- | ----- | ------- |
| init | alpine:3.21 | One-shot: generates secrets on first run |
| db | postgres:17 | PostgreSQL database |
| minio | minio/minio:RELEASE.2025-03-12T18-04-18Z | S3-compatible receipt storage |
| app | ghcr.io/mggarofalo/gas:latest | .NET API + React SPA (port 8080) |

### Startup Order

```
init (generates secrets) → db + minio (wait for healthy) → app
```

## Dockerfile

Three-stage multi-arch build (amd64 + arm64):

1. **client-build** (`node:22-alpine`) — `npm ci && npm run build` produces the React SPA in `/app/client/dist`
2. **api-build** (`dotnet/sdk:10.0`) — `dotnet publish` produces the .NET API in `/app/publish`
3. **runtime** (`dotnet/aspnet:10.0`) — copies API output + SPA into `wwwroot`, runs via `docker-entrypoint.sh`

The runtime image installs `curl` for the health check probe and listens on port 8080.

## Secrets

Secrets are **auto-generated** on first run by the `init` service and stored in a Docker named volume (`secrets`). The entrypoint script (`docker-entrypoint.sh`) reads these files and bridges them to environment variables:

| Secret file | Environment variable |
| ----------- | -------------------- |
| `/secrets/pg_password` | `ConnectionStrings__Default` (full connection string) |
| `/secrets/minio_access_key` | `MinIO__AccessKey` |
| `/secrets/minio_secret_key` | `MinIO__SecretKey` |
| `/secrets/jwt_key` | `Jwt__Key` |
| `/secrets/admin_password` | `AdminSeed__Password` |

To override a secret, write to its file in the volume before starting the stack.

## Environment Variables

Non-secret configuration is set directly in `docker-compose.yml` environment blocks. See `.env.example` for all configurable variables with defaults.

## Health Checks

All services have Docker health checks:

- **db**: `pg_isready -U gas -d gastracker`
- **minio**: `mc ready local`
- **app**: `curl -sf http://localhost:8080/health` — returns JSON with per-dependency status (PostgreSQL, MinIO)

The `app` service waits for `db` and `minio` to be healthy before starting.

## Security Hardening

- All containers drop all Linux capabilities and add back only what's required
- `no-new-privileges:true` on all containers
- App container runs with `tmpfs` on `/tmp` and 512MB memory limit
- Secrets volume is mounted read-only everywhere except the `init` service

## Backup

### PostgreSQL

A `backup` service in Docker Compose runs `pg_dump -Fc` and stores compressed dumps in the `backups` volume. It uses the `backup` profile so it doesn't start with `docker compose up`.

```bash
# Run a one-off backup
docker compose --profile backup run --rm backup

# Schedule daily backups via host cron (2 AM)
# crontab -e
0 2 * * * cd /path/to/gas && docker compose --profile backup run --rm backup >> /var/log/gas-backup.log 2>&1
```

Backups are named `gastracker_YYYY-MM-DD.dump` and rotated after 7 days.

### Restore

```bash
docker compose --profile backup run --rm backup \
  pg_restore -h db -U gas -d gastracker --clean --if-exists \
    /backups/gastracker_2026-03-30.dump
```

### MinIO

Data persisted in Docker volume (`minio-data`). No automated backup — volume-level snapshots or MinIO replication can be added if needed.
