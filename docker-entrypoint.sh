#!/bin/bash
set -e

# ─── Timezone ─────────────────────────────────────────────────────────────────
if [ -n "${TZ}" ] && [ -f "/usr/share/zoneinfo/${TZ}" ]; then
    ln -snf "/usr/share/zoneinfo/${TZ}" /etc/localtime
    echo "${TZ}" > /etc/timezone
    echo "Timezone set to ${TZ}"
fi

# ─── Secrets bridge ──────────────────────────────────────────────────────────
# Read file-based secrets and export as environment variables.

if [ -f /secrets/pg_password ]; then
    PG_PASSWORD="$(cat /secrets/pg_password)"
    export ConnectionStrings__Default="Host=${POSTGRES_HOST:-db};Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB:-gastracker};Username=${POSTGRES_USER:-gas};Password=${PG_PASSWORD}"
    echo "Database connection string loaded from secrets"
else
    echo "Warning: /secrets/pg_password not found, using ConnectionStrings__Default from environment"
fi

if [ -f /secrets/minio_secret_key ]; then
    export MinIO__SecretKey
    MinIO__SecretKey="$(cat /secrets/minio_secret_key)"
    echo "MinIO secret key loaded from secrets"
fi

if [ -f /secrets/minio_access_key ]; then
    export MinIO__AccessKey
    MinIO__AccessKey="$(cat /secrets/minio_access_key)"
    echo "MinIO access key loaded from secrets"
fi

# ─── Run ──────────────────────────────────────────────────────────────────────
echo "Starting Gas Tracker API..."
exec dotnet GasTracker.Api.dll
