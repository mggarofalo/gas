# Raspberry Pi receipt migration: MinIO to S3Mock

Export using the existing MinIO container **before** pulling or replacing the stack.
Then start S3Mock with its new named volume, run the separate importer, verify a
restart, and finally start GAS. There is no application-startup migration.

The export and import scripts use Bash, Docker, jq, curl, GNU coreutils, and flock
(util-linux). Run them on the Pi, using the same account and Docker context that
operate GAS. The new S3Mock image supports 64-bit ARM (`aarch64`), not 32-bit ARM.
The old MinIO container must still run and include `mc`, as the previous stack did.
The export carries that existing native Linux client into the staging directory;
neither migration script pulls images or downloads executables.

## What is preserved

- Every current object in the receipt bucket, including objects not referenced by GAS.
- Exact object keys, bytes, content types, supported HTTP content headers and user metadata.
- Every database receipt reference, checked before export and after import.
- The original MinIO bucket/volume and the export files, left intact for rollback.

S3Mock will assign its own modification timestamps, ETags, and version identifiers.
This migrates current receipt objects, not historical versions, bucket policies, or
lifecycle rules. GAS does not use those features. Verification compares SHA-256,
size and recorded metadata rather than assuming ETags are portable.

## 1. Prepare without changing the running stack

Obtain these three files from the migration release and put them together in a
directory such as `$HOME/gas-migration-scripts`. Copying the scripts does not
require updating the old application or its Compose definition:

- `scripts/export-minio-receipts.sh`
- `scripts/import-s3mock-receipts.sh`
- `scripts/receipt-migration-common.sh`

From the existing GAS Compose directory, prepare a disk-backed staging folder.
Use `/var/tmp` or an attached disk; avoid a RAM-backed `/tmp`. Keep enough free
space for all receipts, the bundled client, a database backup, and one additional
receipt-sized readback file. Do not reboot or clean the staging folder mid-migration.

```bash
set -euo pipefail
umask 077
STAMP=$(date -u +%Y%m%dT%H%M%SZ)
MIGRATION="/var/tmp/gas-migration-$STAMP"
SCRIPTS="$HOME/gas-migration-scripts"
mkdir -p "$MIGRATION"
chmod 700 "$MIGRATION"
uname -m
for cmd in docker jq curl sha256sum flock; do command -v "$cmd"; done
PROJECT=$(docker inspect gas-db --format '{{index .Config.Labels "com.docker.compose.project"}}')
test -n "$PROJECT"
printf '%s\n' "$PROJECT" > "$MIGRATION/compose-project.txt"
docker compose -p "$PROJECT" config > "$MIGRATION/rollback-compose.yml"
docker inspect gas-db gas-minio --format '{{.Name}} {{json .Mounts}}' \
  > "$MIGRATION/original-volumes.txt"
df -h "$MIGRATION"
```

For this migration, the release is `v1.30.0`. Once it is published, the scripts can
be downloaded without pulling any Docker images:

```bash
RELEASE=v1.30.0
mkdir -p "$SCRIPTS"
for file in export-minio-receipts.sh import-s3mock-receipts.sh receipt-migration-common.sh; do
  curl --fail --location --show-error \
    "https://raw.githubusercontent.com/mggarofalo/gas/$RELEASE/scripts/$file" \
    --output "$SCRIPTS/$file"
done
```

Keep the same Compose project name and existing `db-data`, `secrets`, and `dp-keys`
volumes throughout. Changing the project name can make Docker create an empty
database and different secrets instead of using the existing installation.

Pin the old app and MinIO images locally so rollback never needs a registry pull:

```bash
docker image tag "$(docker inspect gas-app --format '{{.Image}}')" "gas-rollback-app:$STAMP"
docker image tag "$(docker inspect gas-minio --format '{{.Image}}')" "gas-rollback-minio:$STAMP"
cat > "$MIGRATION/rollback-images.yml" <<EOF
services:
  app:
    image: gas-rollback-app:$STAMP
    pull_policy: never
  minio:
    image: gas-rollback-minio:$STAMP
    pull_policy: never
EOF
```

Do not run `docker compose pull` against the old definition. Do not remove or prune
its containers/images/volumes. Never use `docker compose down -v` in this procedure.

## 2. Stop GAS, back up the database, and export

Leave PostgreSQL and MinIO running. The scripts require the stopped `gas-app`
container to remain present, so they can verify that application writes are paused.
Also pause any external writers to the receipt bucket or database.

```bash
docker stop gas-app
docker exec gas-db sh -eu -c '
  export PGPASSWORD="$(cat /secrets/pg_password)"
  exec pg_dump -U "${POSTGRES_USER:-gas}" -d "${POSTGRES_DB:-gastracker}" -Fc
' > "$MIGRATION/gastracker.dump.part"
mv "$MIGRATION/gastracker.dump.part" "$MIGRATION/gastracker.dump"

bash "$SCRIPTS/export-minio-receipts.sh" \
  --output "$MIGRATION/export" \
  --bucket gas-receipts

test -s "$MIGRATION/export/COMPLETE"
jq '{bucket, objects: (.objects | length), receipts: (.receiptKeys | length), bytes: ([.objects[].size] | add // 0)}' \
  "$MIGRATION/export/manifest.json"
```

If your bucket is not `gas-receipts`, supply its actual name. Nonstandard container
names can be provided using `--minio-container`, `--db-container`, and `--app-container`.
The database commands use `POSTGRES_USER` and `POSTGRES_DB` inside the existing container.

**Gate:** the export command must exit zero and print `Export COMPLETE`. The manifest
is sealed by `COMPLETE`, contains a SHA-256 for every file, and accounts for every
database receipt key. Missing references or a changing source cause failure.

An interrupted export can be rerun against the same folder while GAS stays stopped.
If the source inventory changed, use a new export folder. Once complete, that folder
is treated as an immutable snapshot and the exporter refuses to replace it.

Only after this gate passes may you stop MinIO:

```bash
docker stop gas-minio
```

## 3. Install the new definition and start S3Mock

Update the checkout/Compose files to a released version containing the S3Mock change.
Do not start GAS yet. The new base Compose file contains **no MinIO service**, so
pulling it does not contact the MinIO registry. It retains the old volume declaration.

For a clean Git checkout, `git fetch --tags` followed by `git checkout v1.30.0`
selects the migration release. Preserve any local Compose customizations rather
than forcing a checkout over them. Use `ghcr.io/mggarofalo/gas:1.30.0` for the app
image during cutover, or confirm that `latest` points to this release. Wait for the
release's Docker Publish workflow to finish before pulling.

Set the following values in `.env` (copy the exact bucket from the export manifest):

```dotenv
S3__Endpoint=s3mock:9090
S3__BucketName=gas-receipts
S3__UseSSL=false
```

The new `S3__*` values replace the old `MinIO__*` settings. Existing secret files
remain intact; the new stack generates `s3_access_key` and `s3_secret_key` if needed.
S3Mock uses its own `s3mock-data` volume and explicitly retains files on exit.

```bash
docker compose -p "$PROJECT" pull
docker compose -p "$PROJECT" \
  -f docker-compose.yml -f docker-compose.migration.yml \
  up -d db s3mock

curl --fail http://127.0.0.1:19090/favicon.ico
```

The migration override exposes only `127.0.0.1:19090`, not the Pi's LAN address.
The normal Compose definition exposes no S3Mock ports. If that port is occupied,
set `S3_MIGRATION_PORT` and pass the matching URL to the importer. Readiness can
take time on a Pi; the importer waits for it.

Do not add `--remove-orphans`: keep the old stopped containers through verification.

## 4. Import into the running S3Mock service

```bash
bash "$SCRIPTS/import-s3mock-receipts.sh" \
  --input "$MIGRATION/export" \
  --endpoint http://127.0.0.1:19090

cat "$MIGRATION/export/verification-report.json"
```

The importer checks the complete local export before uploading, then uploads via
S3 APIs. It never writes directly into either server's data volume. It reads every
destination object back and checks its hash, size and metadata, compares the full
object inventory, and rechecks the database references. Transfers are sequential
to limit memory use on the Pi. A single temporary readback file is reused.

**Gate:** exit zero and a `VERIFIED` summary. Check the report timestamp and manifest
checksum; an older report is not proof that a failed rerun succeeded.

Rerunning is safe while GAS remains stopped: identical destination objects are
verified and skipped. Different contents/metadata or unexpected destination keys
stop the script. Nothing is deleted or silently overwritten. A partial upload can
therefore be resumed by rerunning the same command.

## 5. Prove persistence before starting GAS

```bash
docker compose -p "$PROJECT" \
  -f docker-compose.yml -f docker-compose.migration.yml restart s3mock

bash "$SCRIPTS/import-s3mock-receipts.sh" \
  --input "$MIGRATION/export" \
  --endpoint http://127.0.0.1:19090 \
  --verify-only
```

**Gate:** verification must pass again. `--verify-only` never creates a bucket or
uploads a missing object; it cannot hide persistence failures by restoring data.

Remove the temporary host port by recreating S3Mock with just the base definition,
then start GAS. The named volume is preserved:

```bash
docker compose -p "$PROJECT" -f docker-compose.yml up -d --force-recreate s3mock
docker port gas-s3mock
docker compose -p "$PROJECT" -f docker-compose.yml up -d app
docker compose -p "$PROJECT" ps
curl --fail http://127.0.0.1:8080/health
```

`docker port gas-s3mock` should print no host bindings. Verify several old receipt
photos/PDFs in GAS, then upload and view a new receipt. Confirm Paperless sync still
works if enabled. Keep the export, database backup, old images and MinIO volume
until you have accepted the cutover and established backups of the new volume.

S3Mock does not validate presigned URL signatures. Keep it private and access
receipts through GAS's authenticated endpoint. Pin its version; future S3Mock
upgrades should repeat persistence checks rather than assuming its on-disk format
is compatible.

## Rollback and failure recovery

- **Export fails:** do not pull the new stack. Resolve missing receipts/source changes
  and rerun the exporter. To abandon migration, start the unchanged `gas-app`.
- **Import is interrupted:** keep GAS stopped and rerun the importer. Retain the export.
- **Destination conflict:** inspect it; do not edit the manifest or force an overwrite.
  The failed command leaves existing contents intact. Use a separate empty destination
  only after confirming that it contains no receipts created since cutover.
- **Restart verification fails:** do not start GAS. Check volume mounting and retention
  settings. Rerun import only after fixing persistence, then repeat restart verification.
- **Rollback before accepting new writes:** stop the new app/S3Mock and start the
  original MinIO and app using saved images and the original volumes:

```bash
docker stop gas-app gas-s3mock
docker compose -p "$PROJECT" \
  -f "$MIGRATION/rollback-compose.yml" -f "$MIGRATION/rollback-images.yml" \
  up -d --no-deps --pull never minio app
```

This procedure does not change database receipt paths or require restoring the
database dump. If you have already accepted new uploads/deletions on S3Mock, the
old MinIO bucket is stale: freeze writes and reconcile those differences before
rolling back. Do not blindly restore the old database over newer records.

## Validation performed before release

The automated integration test builds the existing MinIO release from pinned source
for an isolated fixture, exports through `docker exec`, stops the source, imports
into S3Mock, and verifies bytes/metadata and persistence after container recreation.
It covers empty buckets, paginated inventories over 1,000 keys, empty/binary files,
Unicode/quoted/newline keys, missing/changed database references, changed manifests,
incomplete/corrupt exports, idempotent reruns, partial destinations, conflicts, and
unexpected destination objects. It also exercises the application's AWS SDK receipt
operations against S3Mock. Production migration requires no source build or new
MinIO image; it uses the already-running container.

References: [S3Mock configuration](https://github.com/adobe/S3Mock/tree/5.2.3#configuration),
[MinIO object metadata](https://docs.min.io/aistor/reference/cli/mc-stat/).
