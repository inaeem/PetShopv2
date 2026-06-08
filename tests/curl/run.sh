#!/usr/bin/env bash
#
# Runs every use-case script under cases/ and reports an aggregate result.
# Each case runs in its own process, so one failure doesn't stop the rest.
#
#   BASE=http://localhost:5185 PETSHOP_TOKEN="eyJ..." tests/curl/run.sh
#   tests/curl/run.sh cases/create-pet.sh cases/get-pet.sh   # selected case(s)
#
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cases=("$@")
[ ${#cases[@]} -eq 0 ] && cases=("$ROOT"/cases/*.sh)

pass=0; fail=0
for f in "${cases[@]}"; do
  echo "-- $(basename "$f") --"
  if bash "$f"; then pass=$((pass + 1)); else fail=$((fail + 1)); fi
  echo
done

echo "============================"
echo "use cases passed: $pass, failed: $fail"
[ "$fail" -eq 0 ]
