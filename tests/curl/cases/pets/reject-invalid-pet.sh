#!/usr/bin/env bash
# Use case: an invalid create payload is rejected with 400 and validation errors.
source "$(dirname "${BASH_SOURCE[0]}")/../../lib/common.sh"

echo "[pets/reject-invalid-pet]"
call create-invalid 400 "${AUTH[@]}" "${JSON[@]}" -X POST "$BASE/api/pets" -d @"$FIX/create-pet-invalid.json"
expect_json create-invalid '.success == false and (.errors | length) > 0'

finish
