#!/usr/bin/env bash
#
# Export the OpenAPI (Swagger) document to a file: artifacts/swagger.json
#
# Swagger is generated at runtime by the app, but the UI/endpoint are only mapped
# in Development. This script uses the Swashbuckle CLI to emit the document to a
# file directly from the built assembly — handy for sharing with clients or
# importing into Postman/another tool. It runs with migrations/seeding disabled so
# it needs no database.
#
#   scripts/swagger.sh                 # -> artifacts/swagger.json (doc "v1")
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

CONFIG="${CONFIG:-Release}"
OUT="${OUT:-artifacts}"
DOC="${DOC:-v1}"

echo "==> build API ($CONFIG)"
dotnet build src/PetShop.Api -c "$CONFIG" --nologo

echo "==> export OpenAPI doc '$DOC' -> $OUT/swagger.json"
"$SCRIPT_DIR/_openapi.sh" "src/PetShop.Api/bin/$CONFIG/net8.0/PetShop.Api.dll" "$OUT/swagger.json" "$DOC"

echo "==> done: $OUT/swagger.json"
