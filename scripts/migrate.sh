#!/usr/bin/env bash
#
# Apply EF Core migrations to a target database from this machine (it must be able
# to reach the SQL Server). For an existing database that already has the schema,
# baseline it ONCE first (see note below) so tables aren't recreated.
#
# Usage:
#   PETSHOP_CONNECTION='Server=...;Database=PetShop;User Id=...;Password=...;TrustServerCertificate=True;' \
#     scripts/migrate.sh
#
# Existing/pre-populated database (run the baseline once, on a host that has sqlcmd):
#   sqlcmd -S <server> -d PetShop -U <user> -P <pass> -i database/04_baseline_existing_database.sql \
#          -v MigrationId="20260101000000_InitialCreate" ProductVersion="8.0.6"
#   ...then run this script.
#
# Migrations are recorded in __EFMigrationsHistory, so this is idempotent and never
# applies the same migration twice.
#
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

CONNECTION="${PETSHOP_CONNECTION:-}"
if [ -z "$CONNECTION" ]; then
  echo "ERROR: set PETSHOP_CONNECTION to the target connection string." >&2
  echo "Example:" >&2
  echo "  PETSHOP_CONNECTION='Server=qa-sql;Database=PetShop;User Id=app;Password=***;TrustServerCertificate=True;' scripts/migrate.sh" >&2
  exit 1
fi

echo "==> restore local tools (dotnet-ef)"
dotnet tool restore >/dev/null

echo "==> applying migrations to target database"
dotnet ef database update \
  --project src/PetShop.Data \
  --startup-project src/PetShop.Api \
  --connection "$CONNECTION"

echo "==> done"
