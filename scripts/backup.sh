#!/bin/bash
set -euo pipefail

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="/backups/gastracker_${TIMESTAMP}.sql.gz"

export PGPASSWORD
PGPASSWORD="$(cat /secrets/pg_password)"

echo "Starting backup at ${TIMESTAMP}..."
pg_dump -h "${POSTGRES_HOST}" -p "${POSTGRES_PORT}" -U "${POSTGRES_USER}" "${POSTGRES_DB}" \
    | gzip > "${BACKUP_FILE}"

echo "Backup complete: ${BACKUP_FILE} ($(du -h "${BACKUP_FILE}" | cut -f1))"

# Keep only last 30 backups
ls -1t /backups/gastracker_*.sql.gz 2>/dev/null | tail -n +31 | xargs -r rm -f
echo "Cleanup done (keeping last 30 backups)"
