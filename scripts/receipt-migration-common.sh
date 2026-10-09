#!/usr/bin/env bash
# Shared by the two standalone migration commands; no GAS application code needed.
# Variables are shared with the calling scripts.
# shellcheck disable=SC2034,SC2154
set -Eeuo pipefail
umask 077

die() { printf 'ERROR: %s\n' "$*" >&2; exit 1; }
need() { command -v "$1" >/dev/null || die "Required command not found: $1"; }
hash_file() { sha256sum -- "$1" | cut -d ' ' -f1; }
hash_key() { printf '%s' "$1" | sha256sum | cut -d ' ' -f1; }
json_key() { IFS= read -r -d '' key < <(jq -j '.key, "\u0000"' <<< "$1"); }
check_app_stopped() {
    local state
    state=$(docker inspect --format '{{.State.Running}}' "$app_container") ||
        die "Cannot inspect $app_container. Keep its stopped container through verification."
    [[ $state == false ]] || die "Stop $app_container before exporting/importing/verifying receipts."
}
check_bucket() {
    [[ $1 =~ ^[a-z0-9][a-z0-9.-]{1,61}[a-z0-9]$ ]] || die "Invalid bucket name: $1"
}
lock_directory() {
    [[ ! -L $workdir ]] || die "Staging directory must not be a symlink"
    mkdir -p -- "$workdir"
    workdir=$(cd -- "$workdir" && pwd -P)
    exec 9>"$workdir/.migration.lock"
    flock -n 9 || die "Another migration is using $workdir"
}
inventory_json() {
    # mc handles S3 pagination. Never parse human-readable ls output.
    jq -e -s '
      if any(.[]; .status != "success") then error("S3 listing failed") else . end |
      if any(.[]; .type != "file") then error("Unsupported directory record in recursive object listing; refusing an incomplete export") else . end |
      map({key, size, etag, lastModified}) | sort_by(.key) |
      if any(.[]; (.key | type) != "string" or (.key | length) == 0 or
        (.key | contains("\u0000")) or (.size | type) != "number" or .size < 0)
      then error("Invalid object inventory") else . end |
      if (map(.key) | unique | length) != length then error("Duplicate keys") else . end'
}
headers_json() {
    jq -e '
      if .status != "success" then error("Object stat failed") else . end |
      (.metadata // {}) | with_entries(.key |= ascii_downcase) |
      with_entries(select(.key | test("^(content-type|content-encoding|content-language|content-disposition|cache-control|expires|x-amz-meta-[a-z0-9-]+)$"))) |
      if any(.[]; type != "string" or test("[\r\n\u0000]"))
      then error("Invalid metadata header") else . end'
}
database_references() {
    docker exec "$db_container" sh -eu -c '
      if [ -f /secrets/pg_password ]; then export PGPASSWORD="$(cat /secrets/pg_password)"; fi
      exec psql -X -v ON_ERROR_STOP=1 -U "${POSTGRES_USER:-gas}" -d "${POSTGRES_DB:-gastracker}" -At -c "$1"
    ' sh "SELECT COALESCE(json_agg(receipt_path ORDER BY receipt_path), '[]'::json) FROM (SELECT DISTINCT receipt_path FROM fill_ups WHERE receipt_path IS NOT NULL) AS receipts;" |
        jq -e 'if type == "array" and all(.[]; type == "string") then sort else error("Invalid database receipt references") end'
}
verify_references() {
    jq -e -n --slurpfile refs "$1" --slurpfile objects "$2" '
      ($refs[0] - ($objects[0] | map(.key))) as $missing |
      if ($missing | length) == 0 then true else error("Missing database receipt keys: " + ($missing | tojson)) end' >/dev/null
}
