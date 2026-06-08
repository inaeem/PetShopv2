#!/usr/bin/env bash
# Use case: the caller's available pets are returned grouped by category (GET /api/pets/mine).
# A newly-created pet defaults to Available, so its category should appear with the pet in it.
source "$(dirname "${BASH_SOURCE[0]}")/../../lib/common.sh"

echo "[category/available-by-category]"
create_pet >/dev/null
call available-by-category 200 "${AUTH[@]}" "$BASE/api/pets/mine"
expect_json available-by-category '.success == true and (.data | type) == "array"'
# The created pet's category is present, with the pet listed under it.
expect_json available-by-category '[.data[].pets[].name] | index("Rex") != null'

finish
