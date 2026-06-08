#!/usr/bin/env bash
# Use case: a user creates a pet and the response echoes it back with an id.
source "$(dirname "${BASH_SOURCE[0]}")/../lib/common.sh"

echo "[create-pet]"
body="$(new_request_body create-pet)"
call create-pet 201 "${AUTH[@]}" "${JSON[@]}" -X POST "$BASE/api/pets" -d @"$body"
expect_json create-pet '.success == true and .data.name == "Rex" and .data.id > 0'

finish
