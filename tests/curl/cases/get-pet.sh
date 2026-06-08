#!/usr/bin/env bash
# Use case: fetch a single pet by id.
source "$(dirname "${BASH_SOURCE[0]}")/../lib/common.sh"

echo "[get-pet]"
id="$(create_pet)"
call get-pet 200 "${AUTH[@]}" "$BASE/api/pets/$id"
expect_json get-pet ".success == true and .data.id == $id and .data.name == \"Rex\""

finish
