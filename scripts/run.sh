#!/usr/bin/env bash
#
# Run the API locally for a given environment.
#
#   scripts/run.sh                 # Development
#   scripts/run.sh QA              # QA
#   scripts/run.sh Production      # Production
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

ENVIRONMENT="${1:-${ASPNETCORE_ENVIRONMENT:-Development}}"

echo "==> running PetShop.Api (ASPNETCORE_ENVIRONMENT=$ENVIRONMENT)"
ASPNETCORE_ENVIRONMENT="$ENVIRONMENT" dotnet run --project src/PetShop.Api
