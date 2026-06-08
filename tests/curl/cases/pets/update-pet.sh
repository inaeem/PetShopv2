#!/usr/bin/env bash
# Use case: update an existing pet's fields (price changes 650 -> 700).
source "$(dirname "${BASH_SOURCE[0]}")/../../lib/common.sh"

echo "[pets/update-pet]"
id="$(create_pet)"
body="$(new_request_body update-pet)"
call update-pet 200 "${AUTH[@]}" "${JSON[@]}" -X PUT "$BASE/api/pets/$id" -d @"$body"
expect_json update-pet '.success == true and .data.price == 700'

finish
