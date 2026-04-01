#!/bin/bash
# ──────────────────────────────────────────────────────────────────────────────
# Gas Tracker — PostgreSQL backup
#
# Runs pg_dump in custom format (-Fc) and rotates backups older than 7 days.
#
# RESTORE PROCEDURE:
#   docker compose run --rm -v gas_backups:/backups postgres:17 \
#     pg_restore -h db -U gas -d gastracker --clean --if-exists \
#       /backups/gastracker_2026-03-30.dump
#
#   Or from a host-mounted path:
#     docker compose exec db pg_restore -U gas -d gastracker --clean --if-exists \
#       /backups/gastracker_2026-03-30.dump
# ──────────────────────────────────────────────────────────────────────────────
set -euo pipefail

BACKUP_DIR="/backups"
DB_HOST="${POSTGRES_HOST:-db}"
DB_PORT="${POSTGRES_PORT:-5432}"
DB_USER="${POSTGRES_USER:-gas}"
DB_NAME="${POSTGRES_DB:-gastracker}"
RETAIN_DAYS=7

# Read password from secrets volume
if [ -f /secrets/pg_password ]; then
    export PGPASSWORD
    PGPASSWORD="$(cat /secrets/pg_password)"
else
    echo "ERROR: /secrets/pg_password not found" >&2
    exit 1
fi

TIMESTAMP="$(date +%Y-%m-%d)"
FILENAME="gastracker_${TIMESTAMP}.dump"

echo "Starting backup: ${FILENAME}"
pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -Fc "$DB_NAME" \
    > "${BACKUP_DIR}/${FILENAME}"
echo "Backup complete: ${BACKUP_DIR}/${FILENAME} ($(du -h "${BACKUP_DIR}/${FILENAME}" | cut -f1))"

# Rotate old backups
DELETED=$(find "$BACKUP_DIR" -name "gastracker_*.dump" -mtime +"$RETAIN_DAYS" -delete -print | wc -l)
if [ "$DELETED" -gt 0 ]; then
    echo "Rotated ${DELETED} backup(s) older than ${RETAIN_DAYS} days"
fi
