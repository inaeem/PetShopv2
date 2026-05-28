#!/usr/bin/env bash
#
# Produce a deployable release for the Windows/IIS server. Output goes to ./artifacts:
#
#   artifacts/app/            published API (copy to the IIS site folder)
#   artifacts/efbundle.exe    self-contained migration runner (run on the server)
#   artifacts/migrate.sql     idempotent migration script (alternative to efbundle)
#   artifacts/swagger.json    the OpenAPI specification for this build
#   artifacts/database/       baseline script for an existing database
#   artifacts/petshop-api.zip everything above, zipped for hand-off
#
# Usage:
#   scripts/publish.sh                    # framework-dependent (server needs the Hosting Bundle)
#   SELF_CONTAINED=true scripts/publish.sh# bundles the runtime (no .NET needed on the server)
#   RUNTIME=win-x64 scripts/publish.sh    # target server RID (default win-x64)
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

CONFIG="${CONFIG:-Release}"
RUNTIME="${RUNTIME:-win-x64}"
SELF_CONTAINED="${SELF_CONTAINED:-false}"
OUT="${OUT:-artifacts}"
APP_OUT="$OUT/app"

echo "==> restore local tools (dotnet-ef)"
dotnet tool restore >/dev/null

echo "==> clean $OUT"
rm -rf "$OUT"
mkdir -p "$APP_OUT" "$OUT/database"

echo "==> publish API (config=$CONFIG runtime=$RUNTIME self-contained=$SELF_CONTAINED)"
if [ "$SELF_CONTAINED" = "true" ]; then
  dotnet publish src/PetShop.Api -c "$CONFIG" -r "$RUNTIME" --self-contained true -o "$APP_OUT"
else
  dotnet publish src/PetShop.Api -c "$CONFIG" -o "$APP_OUT"
fi

echo "==> generate idempotent migration script -> $OUT/migrate.sql"
dotnet ef migrations script --idempotent \
  --project src/PetShop.Data --startup-project src/PetShop.Api \
  -o "$OUT/migrate.sql"

echo "==> generate migration bundle -> $OUT/efbundle.exe ($RUNTIME)"
dotnet ef migrations bundle --self-contained -r "$RUNTIME" --force \
  --project src/PetShop.Data --startup-project src/PetShop.Api \
  -o "$OUT/efbundle.exe"

echo "==> copy baseline script"
cp database/04_baseline_existing_database.sql "$OUT/database/"

echo "==> generate OpenAPI spec -> $OUT/swagger.json"
"$SCRIPT_DIR/_openapi.sh" "$APP_OUT/PetShop.Api.dll" "$OUT/swagger.json"

if command -v zip >/dev/null 2>&1; then
  echo "==> zip -> $OUT/petshop-api.zip"
  ( cd "$OUT" && zip -rq petshop-api.zip app migrate.sql efbundle.exe swagger.json database )
else
  echo "==> 'zip' not found; skipping archive — hand off the $OUT/ folder instead"
fi

echo "==> done. Release in: $OUT/"
