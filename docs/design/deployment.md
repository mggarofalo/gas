# Deployment

## Docker Compose

Single `docker-compose.yml` runs the full stack locally and in production.

### Services

| Service    | Image                          | Port  |
| ---------- | ------------------------------ | ----- |
| api        | Custom Dockerfile (.NET)       | 5000  |
| frontend   | Custom Dockerfile (nginx)      | 3000  |
| db         | postgres:17                    | 5432  |
| minio      | minio/minio                    | 9000/9001 |

### Compose Topology

```yaml
services:
  db:
    image: postgres:17
    environment:
      POSTGRES_DB: gastracker
      POSTGRES_USER: gas
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - pgdata:/var/lib/postgresql/data
    ports:
      - "5432:5432"

  minio:
    image: minio/minio
    command: server /data --console-address ":9001"
    environment:
      MINIO_ROOT_USER: ${MINIO_ACCESS_KEY}
      MINIO_ROOT_PASSWORD: ${MINIO_SECRET_KEY}
    volumes:
      - miniodata:/data
    ports:
      - "9000:9000"
      - "9001:9001"

  api:
    build:
      context: ./src/GasTracker
      dockerfile: GasTracker.Api/Dockerfile
    environment:
      ConnectionStrings__Default: Host=db;Database=gastracker;Username=gas;Password=${DB_PASSWORD}
      MinIO__Endpoint: minio:9000
      MinIO__AccessKey: ${MINIO_ACCESS_KEY}
      MinIO__SecretKey: ${MINIO_SECRET_KEY}
    ports:
      - "5000:8080"
    depends_on:
      - db
      - minio

  frontend:
    build:
      context: ./src/frontend
    ports:
      - "3000:80"
    depends_on:
      - api

volumes:
  pgdata:
  miniodata:
```

### Frontend Dockerfile

Multi-stage: `node` for build, `nginx` for serving. Nginx proxies `/api/*` to the API service.

### API Dockerfile

Multi-stage: `dotnet/sdk` for build, `dotnet/aspnet` for runtime. EF migrations run on startup via `db.Database.Migrate()`.

## Environment Variables

Managed via `.env` file (gitignored):

```
DB_PASSWORD=changeme
MINIO_ACCESS_KEY=minioadmin
MINIO_SECRET_KEY=changeme
```

## Backup

- PostgreSQL: cron job running `pg_dump` to a mounted volume or remote target
- MinIO: data persisted in Docker volume; can enable MinIO replication for redundancy if needed
