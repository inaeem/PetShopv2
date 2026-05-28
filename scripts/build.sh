#!/usr/bin/env bash
#
# Restore, build and (optionally) test the solution.
#
#   scripts/build.sh                 # Release build + OpenAPI spec + tests
#   CONFIG=Debug scripts/build.sh    # Debug build
#   RUN_TESTS=false scripts/build.sh # skip the e2e tests (they need a SQL Server)
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

CONFIG="${CONFIG:-Release}"
RUN_TESTS="${RUN_TESTS:-true}"
SPEC_OUT="${SPEC_OUT:-artifacts/swagger.json}"

echo "==> restore"
dotnet restore PetShop.sln

echo "==> build ($CONFIG)"
dotnet build PetShop.sln -c "$CONFIG" --no-restore

echo "==> generate OpenAPI spec"
"$SCRIPT_DIR/_openapi.sh" "src/PetShop.Api/bin/$CONFIG/net8.0/PetShop.Api.dll" "$SPEC_OUT"

if [ "$RUN_TESTS" = "true" ]; then
  echo "==> test"
  echo "    (e2e tests need SQL Server/LocalDB; set PETSHOP_TEST_CONNECTION or RUN_TESTS=false)"
  dotnet test PetShop.sln -c "$CONFIG" --no-build
else
  echo "==> tests skipped (RUN_TESTS=false)"
fi

echo "==> done"
