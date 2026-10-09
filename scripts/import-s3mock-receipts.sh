#!/usr/bin/env bash
# Import AFTER S3Mock and its new volume are running. MinIO is not needed.
set -Eeuo pipefail
# shellcheck source=receipt-migration-common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)/receipt-migration-common.sh"

workdir='' endpoint=http://127.0.0.1:19090 app_container=gas-app db_container=gas-db verify_only=false
while (($#)); do
    case "$1" in
        --input) workdir=${2:?}; shift 2 ;;
        --endpoint) endpoint=${2:?}; shift 2 ;;
        --app-container) app_container=${2:?}; shift 2 ;;
        --db-container) db_container=${2:?}; shift 2 ;;
        --verify-only) verify_only=true; shift ;;
        --help) echo 'Usage: import-s3mock-receipts.sh --input DIRECTORY [--endpoint http://127.0.0.1:19090] [--verify-only] [--app-container gas-app] [--db-container gas-db]'; exit 0 ;;
        *) die "Unknown argument: $1" ;;
    esac
done
[[ -n $workdir && -d $workdir ]] || die '--input must point to the completed export directory'
for cmd in docker jq curl sha256sum flock cmp wc; do need "$cmd"; done
endpoint=${endpoint%/}
[[ $endpoint =~ ^http://127\.0\.0\.1:[0-9]+$ ]] || die 'Use the temporary localhost-only S3Mock port (http://127.0.0.1:PORT)'
check_app_stopped
lock_directory
[[ -f $workdir/COMPLETE && -f $workdir/manifest.json ]] || die 'Export is incomplete'
[[ $(hash_file "$workdir/manifest.json") == "$(cat "$workdir/COMPLETE")" ]] || die 'Manifest checksum mismatch'
jq -e '
  .format == 1 and (.bucket | type == "string") and
  (.mcSha256 | test("^[a-f0-9]{64}$")) and (.receiptKeys | type == "array") and
  all(.receiptKeys[]; type == "string") and (.objects | type == "array") and
  all(.objects[];
    (.key | type == "string" and length > 0 and (contains("\u0000") | not)) and
    (.file | test("^objects/[a-f0-9]{64}$")) and (.sha256 | test("^[a-f0-9]{64}$")) and
    (.size | type == "number" and . >= 0 and floor == .) and (.headers | type == "object") and
    all(.headers | to_entries[];
      (.key | test("^(content-type|content-encoding|content-language|content-disposition|cache-control|expires|x-amz-meta-[a-z0-9-]+)$")) and
      (.value | type == "string" and (test("[\r\n\u0000]") | not)))) and
  ((.objects | map(.key) | unique | length) == (.objects | length)) and
  ((.objects | map(.file) | unique | length) == (.objects | length))
' "$workdir/manifest.json" >/dev/null || die 'Invalid manifest'
bucket=$(jq -r .bucket "$workdir/manifest.json")
check_bucket "$bucket"
[[ ! -L $workdir/tools && ! -L $workdir/tools/mc && ! -L $workdir/objects ]] || die 'Unexpected staging symlink'
[[ $(hash_file "$workdir/tools/mc") == "$(jq -r .mcSha256 "$workdir/manifest.json")" ]] || die 'Bundled mc checksum mismatch'

# Validate the complete export before performing any writes to the destination.
while IFS= read -r item; do
    json_key "$item"
    file=$(jq -r .file <<< "$item")
    [[ $file == "objects/$(hash_key "$key")" && ! -L $workdir/$file && -f $workdir/$file ]] || die "Invalid object file: $file"
    [[ $(wc -c < "$workdir/$file") -eq $(jq -r .size <<< "$item") &&
       $(hash_file "$workdir/$file") == "$(jq -r .sha256 <<< "$item")" ]] || die "Exported object checksum/size mismatch: $file"
done < <(jq -c '.objects[]' "$workdir/manifest.json")
jq '.receiptKeys' "$workdir/manifest.json" > "$workdir/import-references.json"
jq '.objects' "$workdir/manifest.json" > "$workdir/import-objects.json"
verify_references "$workdir/import-references.json" "$workdir/import-objects.json"
database_references > "$workdir/current-references.json"
cmp -s "$workdir/import-references.json" "$workdir/current-references.json" || die 'Database receipt references differ from the export; investigate before importing'

local_config=$(mktemp -d)
trap 'rm -rf -- "$local_config"' EXIT
chmod 700 "$workdir/tools/mc"
target_mc() { "$workdir/tools/mc" --config-dir "$local_config" "$@"; }
ready=false
for ((attempt=0; attempt<60; attempt++)); do
    if curl --silent --fail --connect-timeout 2 --max-time 5 "$endpoint/favicon.ico" >/dev/null; then ready=true; break; fi
    sleep 2
done
$ready || die 'S3Mock did not become ready'
target_mc alias set gas-import "$endpoint" gas gas-local-storage >/dev/null
if ! $verify_only; then target_mc mb --ignore-existing "gas-import/$bucket" >/dev/null; fi
target_mc --json ls --recursive "gas-import/$bucket/" | inventory_json > "$workdir/destination-before.json"
jq -e -n --slurpfile dest "$workdir/destination-before.json" --slurpfile objects "$workdir/import-objects.json" \
    '($dest[0] | map(.key)) - ($objects[0] | map(.key)) | length == 0' >/dev/null ||
    die 'Destination contains unexpected objects. Use a dedicated empty receipt bucket or investigate; nothing will be deleted.'

verify_remote() {
    local item=$1 url=$2 remote_hash remote_size
    # Download raw stored bytes (do not use curl --compressed).
    curl --silent --show-error --fail --path-as-is --globoff --connect-timeout 5 --max-time 300 \
        "$url" -o "$workdir/readback.part"
    remote_hash=$(hash_file "$workdir/readback.part")
    remote_size=$(wc -c < "$workdir/readback.part")
    [[ $remote_hash == "$(jq -r .sha256 <<< "$item")" && $remote_size -eq $(jq -r .size <<< "$item") ]] ||
        die "Destination content conflict for $(jq -c .key <<< "$item"); not overwritten"
    target_mc --json stat "gas-import/$bucket/$key" | headers_json > "$workdir/remote-headers.json"
    jq -e -n --argjson item "$item" --slurpfile actual "$workdir/remote-headers.json" \
        '$item.headers | to_entries | all(.[]; .value == $actual[0][.key])' >/dev/null ||
        die "Destination metadata mismatch for $(jq -c .key <<< "$item"); not overwritten"
}

copied=0 skipped=0
while IFS= read -r item; do
    check_app_stopped
    json_key "$item"
    # Preserve slash separators; escape each path segment without double-encoding percent signs.
    url="$endpoint/$bucket/$(jq -rn --arg key "$key" '$key | split("/") | map(@uri) | join("/")')"
    if jq -e --arg key "$key" 'any(.[]; .key == $key)' "$workdir/destination-before.json" >/dev/null; then
        verify_remote "$item" "$url"
        skipped=$((skipped+1))
    else
        $verify_only && die "Missing destination object: $(jq -c .key <<< "$item")"
        headers=()
        while IFS= read -r header; do
            headers+=(--header "$(jq -r '.key + ": " + .value' <<< "$header")")
        done < <(jq -c '.headers | to_entries[]' <<< "$item")
        file=$(jq -r .file <<< "$item")
        curl --silent --show-error --fail --path-as-is --globoff --connect-timeout 5 --max-time 300 \
            --header 'If-None-Match: *' "${headers[@]}" --upload-file "$workdir/$file" "$url" >/dev/null
        verify_remote "$item" "$url"
        copied=$((copied+1))
    fi
done < <(jq -c '.objects[]' "$workdir/manifest.json")

check_app_stopped
target_mc --json ls --recursive "gas-import/$bucket/" | inventory_json > "$workdir/destination-after.json"
jq -e -n --slurpfile dest "$workdir/destination-after.json" --slurpfile objects "$workdir/import-objects.json" \
    '($dest[0] | map({key,size}) | sort_by(.key)) == ($objects[0] | map({key,size}) | sort_by(.key))' >/dev/null || die 'Destination inventory mismatch'
verify_references "$workdir/import-references.json" "$workdir/destination-after.json"
database_references > "$workdir/current-references.json"
cmp -s "$workdir/import-references.json" "$workdir/current-references.json" || die 'Database receipt references changed during import'
jq -n --arg verifiedAt "$(date -u +%FT%TZ)" --arg endpoint "$endpoint" --arg bucket "$bucket" \
    --arg manifestSha256 "$(cat "$workdir/COMPLETE")" --argjson copied "$copied" --argjson verifiedExisting "$skipped" --argjson verifyOnly "$verify_only" \
    '{verifiedAt:$verifiedAt,endpoint:$endpoint,bucket:$bucket,manifestSha256:$manifestSha256,copied:$copied,verifiedExisting:$verifiedExisting,verifyOnly:$verifyOnly}' \
    > "$workdir/verification-report.json.part"
mv -f "$workdir/verification-report.json.part" "$workdir/verification-report.json"
printf 'VERIFIED: %s copied, %s existing objects checked; content, metadata, inventory, and database references match.\n' "$copied" "$skipped"
echo 'Restart S3Mock, rerun with --verify-only, then start GAS. Preserve the export and old MinIO volume.'
