#!/usr/bin/env bash
# Use case: the API rejects requests without a valid token, accepts them with one.
source "$(dirname "${BASH_SOURCE[0]}")/../lib/common.sh"

echo "[auth-required]"
# No Authorization header -> 401 envelope
call unauth-list 401 "$BASE/api/pets?page=1&pageSize=5"
expect_json unauth-list '.success == false and (.message | length) > 0'

# Valid token -> 200 with a list payload
call auth-list 200 "${AUTH[@]}" "$BASE/api/pets?page=1&pageSize=5"
expect_json auth-list '.success == true and (.data.items | type) == "array"'

finish
