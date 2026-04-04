# Deployment

## Architecture

Single `docker-compose.yml` runs the full stack. The API and React SPA are combined into one container -- .NET serves the SPA from `wwwroot` alongside the API endpoints. No nginx reverse proxy needed.

## Docker Compose Services

### init

- Alpine container, runs once
- Generates random secrets (48 chars each) into a shared Docker volume: `pg_password`, `minio_access_key`, `minio_secret_key`, `jwt_key`
- Admin password: 44 alphanum + `@A1!` suffix (meets Identity password policy)
- Files created with `chmod 644`, idempotent (skips existing)

### db

- PostgreSQL 17
- Uses `POSTGRES_PASSWORD_FILE` pointing to shared secret
- Health check: `pg_isready`
- Persistent volume for data

### minio

- MinIO (S3-compatible object storage)
- Reads credentials from shared secrets at startup via entrypoint script
- Console on port 9001 (not exposed by default)
- Persistent volume for data

### app

- Built from multi-stage Dockerfile
- `docker-entrypoint.sh` reads secret files and exports as environment variables
- Port 8080
- Depends on healthy db + minio
- Resource limits: 512MB RAM, 1 CPU
- tmpfs on /tmp

### backup (profile: backup)

- PostgreSQL 17 image running backup script
- Only runs when explicitly activated via `--profile backup`

## Dockerfile (Multi-Stage)

### Stage 1: client-build

- `node:22-alpine`
- `npm ci` + `npm run build` for the React SPA
- Output: `dist/` directory

### Stage 2: api-build

- `mcr.microsoft.com/dotnet/sdk:10.0`
- Restore + publish for the target architecture (amd64/arm64)
- Output: `/app/publish/` directory

### Stage 3: runtime

- `mcr.microsoft.com/dotnet/aspnet:10.0`
- Copies published API + built SPA into `/app/wwwroot/`
- Installs `curl` for health check
- HEALTHCHECK: `curl -sf http://localhost:8080/health`
- ENTRYPOINT: `./docker-entrypoint.sh`

## docker-entrypoint.sh

1. Sets timezone from `$TZ` if configured
2. Reads `/secrets/pg_password` -> constructs `ConnectionStrings__Default`
3. Reads `/secrets/minio_access_key` -> `MinIO__AccessKey`
4. Reads `/secrets/minio_secret_key` -> `MinIO__SecretKey`
5. Reads `/secrets/jwt_key` -> `Jwt__Key` (strips whitespace)
6. Reads `/secrets/admin_password` -> `AdminSeed__Password` (strips newlines)
7. Exec `dotnet GasTracker.Api.dll`

## CI/CD

### ci.yml (on push/PR to main)

- **backend**: Setup .NET 10, restore, build, test
- **frontend**: Setup Node 22, npm ci, tsc --noEmit, npm run build

### docker-publish.yml (on tag v*.*.*)

- Build per-architecture images (amd64, arm64) with QEMU emulation
- Push to GHCR as `ghcr.io/mggarofalo/gas`
- Merge manifests into multi-arch tags (latest, semver, sha)
- Trivy security scan on the merged image

### release-please.yml

- Automated versioning and changelog generation

## Security Hardening

- All containers: `cap_drop: ALL` with minimal capabilities added back
- `security_opt: no-new-privileges:true`
- Secrets in a named Docker volume (never in environment variables in compose file)
- YNAB API token encrypted at rest via Data Protection API (keys persisted to `/dp-keys` volume)
- Structured logging with size limits (50MB, 3 files)
- Resource limits on app container
- tmpfs for temporary files

## Published Image

- Registry: `ghcr.io/mggarofalo/gas`
- Architectures: `linux/amd64`, `linux/arm64`
- Tags: `latest`, `{major}.{minor}`, `{major}.{minor}.{patch}`, `sha-{commit}`
