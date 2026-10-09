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

# Prefer new names; existing installations can reuse the legacy secret files.
for kind in access_key secret_key; do
    secret_file="/secrets/s3_${kind}"
    [ -f "$secret_file" ] || secret_file="/secrets/minio_${kind}"
    if [ -f "$secret_file" ]; then
        case "$kind" in
            access_key) export S3__AccessKey="$(cat "$secret_file")" ;;
            secret_key) export S3__SecretKey="$(cat "$secret_file")" ;;
        esac
    fi
done

if [ -f /secrets/jwt_key ]; then
    export Jwt__Key
    Jwt__Key="$(tr -d '[:space:]' < /secrets/jwt_key)"
    echo "JWT signing key loaded from secrets"
fi

if [ -f /secrets/admin_password ]; then
    export AdminSeed__Password
    AdminSeed__Password="$(tr -d '\n\r' < /secrets/admin_password)"
    echo "Admin seed password loaded from secrets"
fi

# ─── Run ──────────────────────────────────────────────────────────────────────
echo "Starting Gas Tracker API..."
exec dotnet GasTracker.Api.dll
