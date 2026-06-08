#!/usr/bin/env bash
# Use case: search via the dbo.usp_SearchPets stored procedure finds a pet by term.
source "$(dirname "${BASH_SOURCE[0]}")/../lib/common.sh"

echo "[search-pets]"
create_pet >/dev/null
call search 200 "${AUTH[@]}" "$BASE/api/pets/search?term=Rex"
expect_json search '.success == true and ([.data[].name] | index("Rex") != null)'

finish
