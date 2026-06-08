#!/usr/bin/env bash
# Use case: a freshly-created pet appears in the owner's paged list.
source "$(dirname "${BASH_SOURCE[0]}")/../lib/common.sh"

echo "[list-pets]"
create_pet >/dev/null
call list-pets 200 "${AUTH[@]}" "$BASE/api/pets?page=1&pageSize=50"
expect_json list-pets '.success == true and ([.data.items[].name] | index("Rex") != null)'

finish
