#!/usr/bin/env bash
# Use case: an Admin deletes a pet, and it is gone (404) afterwards.
source "$(dirname "${BASH_SOURCE[0]}")/../../lib/common.sh"

echo "[pets/delete-pet]"
id="$(create_pet)"
call delete-pet 200 "${AUTH[@]}" -X DELETE "$BASE/api/pets/$id"
expect_json delete-pet '.success == true'

# It should no longer be retrievable.
call get-deleted 404 "${AUTH[@]}" "$BASE/api/pets/$id"
expect_json get-deleted '.success == false'

finish
