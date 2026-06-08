#!/usr/bin/env bash
#
# Runs use-case scripts under cases/ (recursively, grouped by domain folder) and
# reports an aggregate result. Each case runs in its own process, so one failure
# doesn't stop the rest.
#
#   tests/curl/run.sh                       # every case in every group
#   tests/curl/run.sh cases/pets            # one whole group
#   tests/curl/run.sh cases/pets/get-pet.sh # a single case
#   BASE=http://localhost:5185 PETSHOP_TOKEN="eyJ..." tests/curl/run.sh
#
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Resolve args (files or group folders) into a flat, sorted list of case scripts.
args=("$@")
[ ${#args[@]} -eq 0 ] && args=("$ROOT/cases")
cases=()
for a in "${args[@]}"; do
  if [ -d "$a" ]; then
    while IFS= read -r f; do cases+=("$f"); done < <(find "$a" -name '*.sh' | sort)
  else
    cases+=("$a")
  fi
done

pass=0; fail=0; group=""
for f in "${cases[@]}"; do
  g="$(basename "$(dirname "$f")")"
  if [ "$g" != "$group" ]; then group="$g"; echo "=== $group ==="; fi
  echo "-- $(basename "$f") --"
  if bash "$f"; then pass=$((pass + 1)); else fail=$((fail + 1)); fi
  echo
done

echo "============================"
echo "use cases passed: $pass, failed: $fail"
[ "$fail" -eq 0 ]
