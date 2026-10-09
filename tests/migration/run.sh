#!/usr/bin/env bash
# Real MinIO -> export directory -> S3Mock integration test. No production resources.
# Build the two native test binaries from pinned upstream tags first (see CI).
set -Eeuo pipefail
repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)
tools=${MIGRATION_TEST_TOOLS:?Set MIGRATION_TEST_TOOLS to a directory containing native Linux minio and mc binaries}
testdir=$(mktemp -d)
prefix="gas-migration-test-$$"
source_container="$prefix-source" target_container="$prefix-target" db_container="$prefix-db" app_container="$prefix-app"
volume="$prefix-data"
mock_image=adobe/s3mock:5.2.3@sha256:ab01a6946750f451ca215a47e91030695b260e4003b8a5a6201d25029b8fca92
cleanup() {
    docker rm -f "$source_container" "$target_container" "$db_container" "$app_container" >/dev/null 2>&1 || true
    docker volume rm "$volume" >/dev/null 2>&1 || true
    rm -rf -- "$testdir"
}
trap cleanup EXIT
expect_failure() {
    if "$@" > "$testdir/expected-failure.log" 2>&1; then
        cat "$testdir/expected-failure.log"; echo 'Expected command to fail' >&2; exit 1
    fi
}
wait_http() {
    for ((i=0;i<90;i++)); do
        if curl -fsS "$1" >/dev/null 2>&1; then return; fi
        sleep 1
    done
    docker logs "$target_container"; return 1
}
chmod +x "$tools/mc" "$tools/minio"

# The source container models the existing MinIO image: MinIO and mc already inside
# it, accessible via docker exec. Test images never ship with the production stack.
docker create --name "$source_container" --entrypoint /usr/local/bin/minio \
    -p 127.0.0.1::9000 -e MINIO_ROOT_USER=fixture -e MINIO_ROOT_PASSWORD=fixture-password \
    "$mock_image" server /tmp/minio-data >/dev/null
docker cp "$tools/minio" "$source_container:/usr/local/bin/minio" >/dev/null
docker cp "$tools/mc" "$source_container:/usr/local/bin/mc" >/dev/null
docker start "$source_container" >/dev/null
source_endpoint="http://$(docker port "$source_container" 9000)"
wait_http "$source_endpoint/minio/health/live"

docker run -d --name "$target_container" -p 127.0.0.1::9090 \
    -e COM_ADOBE_TESTING_S3MOCK_STORE_ROOT=/s3mockroot \
    -e COM_ADOBE_TESTING_S3MOCK_STORE_RETAIN_FILES_ON_EXIT=true \
    -v "$volume:/s3mockroot" "$mock_image" >/dev/null
endpoint="http://$(docker port "$target_container" 9090)"
wait_http "$endpoint/favicon.ico"
docker run -d --name "$db_container" -e POSTGRES_USER=gas -e POSTGRES_DB=gastracker \
    -e POSTGRES_PASSWORD=fixture-password postgres:17 >/dev/null
for ((i=0;i<60;i++)); do
    if docker exec "$db_container" pg_isready -U gas -d gastracker >/dev/null 2>&1; then break; fi
    sleep 1
done
docker create --name "$app_container" --entrypoint /bin/sh "$mock_image" -c 'sleep 3600' >/dev/null
sql() { docker exec -i "$db_container" psql -X -v ON_ERROR_STOP=1 -U gas -d gastracker "$@"; }
sql -c 'CREATE TABLE fill_ups (receipt_path text);' >/dev/null
mc() { "$tools/mc" --config-dir "$testdir/mc-config" "$@"; }
mc alias set source "$source_endpoint" fixture fixture-password >/dev/null
mc alias set target "$endpoint" fixture fixture-password >/dev/null
mc mb source/gas-receipts >/dev/null

echo 'Empty installation and bucket'
mc mb source/empty-receipts >/dev/null
bash "$repo/scripts/export-minio-receipts.sh" --output "$testdir/empty-export" --bucket empty-receipts \
    --minio-container "$source_container" --db-container "$db_container" --app-container "$app_container"
bash "$repo/scripts/import-s3mock-receipts.sh" --input "$testdir/empty-export" --endpoint "$endpoint" \
    --db-container "$db_container" --app-container "$app_container"

echo 'Paginated S3 inventory includes more than 1,000 keys'
mkdir "$testdir/pagination"
for ((i=0;i<1005;i++)); do printf x > "$testdir/pagination/$i"; done
mc mb source/pagination >/dev/null
mc mirror "$testdir/pagination" source/pagination >/dev/null
(
    # shellcheck source=receipt-migration-common.sh
    source "$repo/scripts/receipt-migration-common.sh"
    mc --json ls --recursive source/pagination/ | inventory_json | jq -e 'length == 1005' >/dev/null
)

key='vehicle/fillup/receipt "quoted" #%? café.jpg'
newline_key=$'vehicle/fillup/line\nbreak.pdf'
printf '\000\001\377binary receipt\n' > "$testdir/receipt"
printf 'PDF fixture\n' > "$testdir/pdf"
: > "$testdir/empty"
mc cp --attr 'Content-Type=image/jpeg;X-Amz-Meta-Fixture=original;Cache-Control=no-cache' "$testdir/receipt" "source/gas-receipts/$key" >/dev/null
mc cp --attr 'Content-Type=application/pdf' "$testdir/pdf" "source/gas-receipts/$newline_key" >/dev/null
mc cp --attr 'Content-Type=image/png' "$testdir/empty" source/gas-receipts/empty.png >/dev/null
jq -n --arg a "$key" --arg b "$newline_key" '[$a,$b,"empty.png"]' > "$testdir/keys.json"
# Dollar quoting keeps the literal quotes and escaped newlines in JSON intact.
# shellcheck disable=SC2016
{ printf 'INSERT INTO fill_ups SELECT jsonb_array_elements_text($fixture$'; cat "$testdir/keys.json"; printf '$fixture$::jsonb);\n'; } | sql >/dev/null
export_args=(--output "$testdir/export" --minio-container "$source_container" --db-container "$db_container" --app-container "$app_container")
import_args=(--input "$testdir/export" --endpoint "$endpoint" --db-container "$db_container" --app-container "$app_container")

echo 'Reject a live application'
docker start "$app_container" >/dev/null
expect_failure bash "$repo/scripts/export-minio-receipts.sh" "${export_args[@]}"
docker stop -t 1 "$app_container" >/dev/null

echo 'Reject missing database receipt references'
sql -c "INSERT INTO fill_ups VALUES ('missing.jpg');" >/dev/null
expect_failure bash "$repo/scripts/export-minio-receipts.sh" "${export_args[@]}"
sql -c "DELETE FROM fill_ups WHERE receipt_path = 'missing.jpg';" >/dev/null

echo 'Export current objects, including empty, binary, Unicode and newline keys'
bash "$repo/scripts/export-minio-receipts.sh" "${export_args[@]}"
[[ $(jq '.objects | length' "$testdir/export/manifest.json") == 3 ]]
expect_failure bash "$repo/scripts/export-minio-receipts.sh" "${export_args[@]}"

echo 'Import must work with the original MinIO container stopped'
docker stop -t 1 "$source_container" >/dev/null
echo 'Reject incomplete export before creating the destination bucket'
mv "$testdir/export/COMPLETE" "$testdir/export/COMPLETE.saved"
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
mv "$testdir/export/COMPLETE.saved" "$testdir/export/COMPLETE"

echo 'Reject corrupted local payload before upload'
file=$(jq -r '.objects[0].file' "$testdir/export/manifest.json")
cp "$testdir/export/$file" "$testdir/original"
printf corrupt >> "$testdir/export/$file"
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
cp "$testdir/original" "$testdir/export/$file"

echo 'Reject a changed manifest'
cp "$testdir/export/manifest.json" "$testdir/manifest.original"
printf ' ' >> "$testdir/export/manifest.json"
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
cp "$testdir/manifest.original" "$testdir/export/manifest.json"

echo 'Reject changed database references'
sql -c "INSERT INTO fill_ups VALUES ('new-reference.jpg');" >/dev/null
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
sql -c "DELETE FROM fill_ups WHERE receipt_path = 'new-reference.jpg';" >/dev/null

echo 'Import and read back all bytes and metadata'
bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
echo 'Idempotent rerun'
bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
[[ $(jq .copied "$testdir/export/verification-report.json") == 0 ]]

echo 'Resume partial destination'
mc rm target/gas-receipts/empty.png >/dev/null
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}" --verify-only
bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
[[ $(jq .copied "$testdir/export/verification-report.json") == 1 ]]

echo 'Never overwrite a conflicting object'
mc cp "$testdir/pdf" target/gas-receipts/empty.png >/dev/null
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
mc cat target/gas-receipts/empty.png | cmp - "$testdir/pdf"
mc rm target/gas-receipts/empty.png >/dev/null
bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"

echo 'Never overwrite conflicting metadata on identical bytes'
mc cp --attr 'Content-Type=application/pdf' "$testdir/empty" target/gas-receipts/empty.png >/dev/null
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
mc rm target/gas-receipts/empty.png >/dev/null
bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"

echo 'Reject unexpected destination objects'
mc cp "$testdir/empty" target/gas-receipts/unexpected >/dev/null
expect_failure bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}"
mc rm target/gas-receipts/unexpected >/dev/null

echo 'Recreate the destination container with the same named volume'
docker rm -f "$target_container" >/dev/null
docker run -d --name "$target_container" -p 127.0.0.1::9090 \
    -e COM_ADOBE_TESTING_S3MOCK_STORE_ROOT=/s3mockroot \
    -e COM_ADOBE_TESTING_S3MOCK_STORE_RETAIN_FILES_ON_EXIT=true \
    -v "$volume:/s3mockroot" "$mock_image" >/dev/null
endpoint="http://$(docker port "$target_container" 9090)"
import_args=(--input "$testdir/export" --endpoint "$endpoint" --db-container "$db_container" --app-container "$app_container")
bash "$repo/scripts/import-s3mock-receipts.sh" "${import_args[@]}" --verify-only
[[ $(jq .verifyOnly "$testdir/export/verification-report.json") == true ]]
echo 'Check the application AWS SDK against S3Mock'
cd "$repo"
S3MOCK_TEST_ENDPOINT="$endpoint" dotnet test tests/GasTracker.Tests/GasTracker.Tests.csproj -c Release --no-build --nologo --filter FullyQualifiedName~S3ReceiptStoreSmokeTests
echo 'Migration integration tests PASSED'
