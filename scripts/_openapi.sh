#!/usr/bin/env bash
#
# Internal helper: emit the OpenAPI (Swagger) document from an already-built API
# assembly. Runs with migrations/seeding disabled, so it needs no database.
#
# Usage: scripts/_openapi.sh <path-to-PetShop.Api.dll> <output-file> [doc-name]
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

DLL="${1:?usage: _openapi.sh <dll> <output-file> [doc]}"
OUTFILE="${2:?usage: _openapi.sh <dll> <output-file> [doc]}"
DOC="${3:-v1}"

dotnet tool restore >/dev/null
mkdir -p "$(dirname "$OUTFILE")"

ASPNETCORE_ENVIRONMENT=Production \
Database__ApplyMigrationsOnStartup=false \
Database__SeedAdmin=false \
  dotnet swagger tofile --output "$OUTFILE" "$DLL" "$DOC"

echo "    OpenAPI spec written: $OUTFILE"
