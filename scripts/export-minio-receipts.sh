#!/usr/bin/env bash
# Export BEFORE replacing/pulling the stack. Uses only the existing containers.
set -Eeuo pipefail
# shellcheck source=receipt-migration-common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/receipt-migration-common.sh"

workdir='' bucket=gas-receipts minio_container=gas-minio db_container=gas-db app_container=gas-app
while (($#)); do
    case "$1" in
        --output) workdir=${2:?}; shift 2 ;;
        --bucket) bucket=${2:?}; shift 2 ;;
        --minio-container) minio_container=${2:?}; shift 2 ;;
        --db-container) db_container=${2:?}; shift 2 ;;
        --app-container) app_container=${2:?}; shift 2 ;;
        --help) echo 'Usage: export-minio-receipts.sh --output DIRECTORY [--bucket gas-receipts] [--minio-container gas-minio] [--db-container gas-db] [--app-container gas-app]'; exit 0 ;;
        *) die "Unknown argument: $1" ;;
    esac
done
[[ -n $workdir ]] || die '--output is required (use a disk-backed directory, not a RAM-backed /tmp)'
for cmd in docker jq sha256sum flock cmp wc; do need "$cmd"; done
check_bucket "$bucket"
check_app_stopped
lock_directory
[[ ! -e $workdir/COMPLETE ]] || die 'Export already complete. Keep it intact; use a different output directory for a new snapshot.'
mkdir -p "$workdir/objects" "$workdir/tools"
[[ ! -L $workdir/objects && ! -L $workdir/tools ]] || die 'Staging subdirectories must not be symlinks'

remote_config=$(docker exec "$minio_container" mktemp -d /tmp/gas-export.XXXXXXXX)
[[ $remote_config =~ ^/tmp/gas-export\.[a-zA-Z0-9]+$ ]] || die 'Unexpected temporary config path'
cleanup() {
    # Only remove the unique mc configuration created by this invocation, never source data.
    docker exec "$minio_container" rm -rf -- "$remote_config" >/dev/null 2>&1 || true
}
trap cleanup EXIT
docker exec "$minio_container" sh -eu -c '
    access=${MINIO_ROOT_USER:-}; secret=${MINIO_ROOT_PASSWORD:-}
    if [ -f /secrets/minio_access_key ]; then access=$(cat /secrets/minio_access_key); fi
    if [ -f /secrets/minio_secret_key ]; then secret=$(cat /secrets/minio_secret_key); fi
    [ -n "$access" ] && [ -n "$secret" ]
    mc --config-dir "$1" alias set gas-export http://127.0.0.1:9000 "$access" "$secret" >/dev/null
' sh "$remote_config"
source_mc() { docker exec "$minio_container" mc --config-dir "$remote_config" "$@"; }

# Carry the already-installed, native Linux mc binary through cutover. No download/pull
# is needed for either script; its checksum is recorded in the sealed manifest.
mc_path=$(docker exec "$minio_container" sh -c 'command -v mc')
[[ $mc_path == /* ]] || die 'mc is not available in the existing MinIO container'
docker cp -L "$minio_container:$mc_path" "$workdir/tools/mc.part"
chmod 700 "$workdir/tools/mc.part"
mv -f "$workdir/tools/mc.part" "$workdir/tools/mc"

source_mc --json ls --recursive "gas-export/$bucket/" | inventory_json > "$workdir/source-inventory.json.part"
if [[ -f $workdir/source-inventory.json ]]; then
    cmp -s "$workdir/source-inventory.json" "$workdir/source-inventory.json.part" ||
        die 'Source changed since the interrupted export. Use a new output directory.'
fi
mv -f "$workdir/source-inventory.json.part" "$workdir/source-inventory.json"
database_references > "$workdir/receipt-keys.json"
verify_references "$workdir/receipt-keys.json" "$workdir/source-inventory.json"
required=$(jq '[.[].size] | add // 0' "$workdir/source-inventory.json")
printf 'Exporting %s objects (%s bytes) to %s\n' "$(jq length "$workdir/source-inventory.json")" "$required" "$workdir"

: > "$workdir/objects.jsonl.part"
while IFS= read -r item; do
    check_app_stopped
    json_key "$item"
    file="objects/$(hash_key "$key")"
    [[ ! -L $workdir/$file && ! -L $workdir/$file.part ]] || die 'Unexpected object symlink'
    size=$(jq -r .size <<< "$item")
    source_mc --json stat "gas-export/$bucket/$key" | headers_json > "$workdir/headers.json.part"
    # Hash a fresh read even when resuming: never trust size or ETag alone.
    remote_hash=$(source_mc cat "gas-export/$bucket/$key" | sha256sum | cut -d ' ' -f1)
    if [[ ! -f $workdir/$file ]] || [[ $(hash_file "$workdir/$file") != "$remote_hash" ]]; then
        source_mc cat "gas-export/$bucket/$key" > "$workdir/$file.part"
        [[ $(hash_file "$workdir/$file.part") == "$remote_hash" ]] || die "Source content changed while downloading: $file"
        mv -f "$workdir/$file.part" "$workdir/$file"
    fi
    [[ $(wc -c < "$workdir/$file") -eq $size ]] || die "Object size mismatch: $file"
    jq -cn --arg key "$key" --arg file "$file" --arg sha256 "$remote_hash" --argjson size "$size" \
        --slurpfile headers "$workdir/headers.json.part" \
        '{key:$key,file:$file,size:$size,sha256:$sha256,headers:$headers[0]}' >> "$workdir/objects.jsonl.part"
done < <(jq -c '.[]' "$workdir/source-inventory.json")

check_app_stopped
source_mc --json ls --recursive "gas-export/$bucket/" | inventory_json > "$workdir/source-inventory.after.json"
cmp -s "$workdir/source-inventory.json" "$workdir/source-inventory.after.json" || die 'Source inventory changed during export'
database_references > "$workdir/receipt-keys.after.json"
cmp -s "$workdir/receipt-keys.json" "$workdir/receipt-keys.after.json" || die 'Database receipt references changed during export'

jq -s --arg bucket "$bucket" --arg createdAt "$(date -u +%FT%TZ)" --arg mcSha256 "$(hash_file "$workdir/tools/mc")" \
    --slurpfile refs "$workdir/receipt-keys.json" \
    '{format:1,bucket:$bucket,createdAt:$createdAt,mcSha256:$mcSha256,receiptKeys:$refs[0],objects:.}' \
    "$workdir/objects.jsonl.part" > "$workdir/manifest.json.part"
mv -f "$workdir/manifest.json.part" "$workdir/manifest.json"
hash_file "$workdir/manifest.json" > "$workdir/COMPLETE.part"
mv -f "$workdir/COMPLETE.part" "$workdir/COMPLETE"
printf 'Export COMPLETE: %s objects; all receipt references and source SHA-256 hashes verified.\n' "$(jq '.objects | length' "$workdir/manifest.json")"
echo 'Keep this directory, the stopped GAS container, and the original MinIO volume until cutover verification passes.'
