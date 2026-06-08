# Curl smoke tests

Black-box HTTP checks for the PetShop API, driven by `curl` + `jq`. Each **use case** is its
own script under `cases/`; `run.sh` executes them all and aggregates pass/fail.

Runs in any bash: macOS, Linux, and the **VS Code / Git Bash terminal on Windows** (Git for
Windows bundles `curl`). Response bodies are validated with `jq`.

```
tests/curl/
├── run.sh                # runner
├── lib/common.sh         # shared config + helpers (token, call, expect_json, new_request_body)
├── cases/*.sh            # one file per use case
├── fixtures/*.json       # request bodies (curl -d @file); CATEGORY_ID injected at runtime
├── responses/            # captured response bodies (gitignored, auto-created)
├── env.example.sh        # config template -> copy to env.sh
├── env.sh                # your config: base url, token, headers, certs (gitignored)
└── token.txt             # your bearer token (gitignored; optional)
```

## Configuration — `env.sh`

Copy the template and edit; `lib/common.sh` sources it automatically:

```bash
cp tests/curl/env.example.sh tests/curl/env.sh
```

| Setting | Purpose |
|---------|---------|
| `BASE` | API base URL (default `http://localhost:5185`) |
| `TOKEN` | Bearer token (or use `$PETSHOP_TOKEN` / `token.txt`) |
| `CATEGORY_ID` | An existing category id used by the create/update bodies (default `1`) |
| `EXTRA_HEADERS` | Bash array of headers sent on **every** request, e.g. `"X-Api-Version: 1"` |
| `CLIENT_CERT_PEM` | mTLS client certificate (PEM) → `curl --cert` |
| `CLIENT_KEY_PEM` | Private key (PEM) if separate from the cert → `curl --key` |
| `CA_CERT_PEM` | CA bundle to verify the server (PEM) → `curl --cacert` |
| `INSECURE` | `1` to skip TLS verification (dev self-signed) → `curl -k` |

`$BASE` and `$PETSHOP_TOKEN` from the shell environment override the file (handy for CI).
Headers and cert options are applied to every request — authenticated or not.

## Prerequisites

1. **`curl` and `jq`.** `curl` is built into macOS/Linux and Git for Windows; install `jq`
   (`brew install jq` / `choco install jq` / `apt install jq`).
2. **The API is running** and reachable at `$BASE`:
   ```bash
   dotnet run --project src/PetShop.Api
   ```
3. **An existing category.** There is no seed step and no category-creation endpoint, so the
   target database must already contain a category. Set `CATEGORY_ID` in `env.sh` to its id.
4. **A bearer token.** The harness does not mint one — the API validates an externally-issued
   RS256 JWT, which `curl` cannot sign. Provide it via `env.sh` `TOKEN`, `$PETSHOP_TOKEN`, or
   a `tests/curl/token.txt` file. The token must carry an `email` claim (pets are owned
   per-caller) and the `Admin` role (for the delete case).

## Running

```bash
# all use cases
tests/curl/run.sh

# selected case(s)
tests/curl/run.sh cases/create-pet.sh cases/get-pet.sh

# a single case directly
tests/curl/cases/update-pet.sh

# override config inline
BASE=http://localhost:5185 PETSHOP_TOKEN="eyJ..." tests/curl/run.sh
```

## Use cases

| Case file | What it checks |
|-----------|----------------|
| `auth-required` | No token → 401 envelope; valid token → 200 list |
| `create-pet` | `POST /api/pets` → 201, body echoes the pet with an id |
| `reject-invalid-pet` | Invalid payload → 400 with `errors` |
| `list-pets` | A created pet appears in the paged list |
| `get-pet` | `GET /api/pets/{id}` returns the pet |
| `update-pet` | `PUT` changes price 650 → 700 |
| `search-pets` | `GET /api/pets/search` (stored proc) finds it |
| `delete-pet` | Admin `DELETE` → 200, then `GET` → 404 |
| `mtls-handshake` | With `CLIENT_CERT_PEM` set over HTTPS: TLS handshake succeeds and the cert is accepted (skips if mTLS isn't configured) |

Each case is **self-contained** — those needing an existing pet create one first (via the
`create_pet` helper), so you can run any single case in isolation and in any order.

## How a case works

```bash
body="$(new_request_body create-pet)"
call create-pet 201 "${AUTH[@]}" "${JSON[@]}" -X POST "$BASE/api/pets" -d @"$body"
expect_json create-pet '.success == true and .data.name == "Rex" and .data.id > 0'
```

`call` sends the request (prepending the shared headers/cert options) and asserts the HTTP
status, capturing the body to `responses/<name>.json`. `expect_json` then asserts on that body
with a `jq` filter (and fails if it isn't valid JSON). Each case exits non-zero if any check
fails, so `run.sh` — and CI — can gate on it.

## Notes

- **State accumulates.** Cases create pets but only `delete-pet` cleans up, so re-runs leave
  extra "Rex" rows. Assertions use "contains", not exact counts, so that's harmless.
- **Line endings.** `.gitattributes` pins `*.sh` to LF so the shebang isn't broken by a CRLF
  checkout on Windows.
- **Responses** land in `responses/` (gitignored) — handy for debugging a failed assertion.
