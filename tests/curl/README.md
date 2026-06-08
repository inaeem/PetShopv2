# Curl smoke tests (Windows / PowerShell)

Black-box HTTP checks for the PetShop API, driven by `curl.exe` and PowerShell.
Each **use case** is its own script under `cases/`; `run.ps1` executes them all and
aggregates pass/fail.

HTTP via `curl.exe` (built into Windows 10 1803+); response validation via PowerShell's
native `ConvertFrom-Json`. **No bash and no jq required.**

```
tests/curl/
├── run.ps1               # runner
├── lib/common.ps1        # shared config + helpers (token, Invoke-Api, Expect-Json, New-Pet)
├── cases/*.ps1           # one file per use case
├── fixtures/*.json       # request bodies (curl -d @file); $CategoryId injected at runtime
├── responses/            # captured response bodies (gitignored, auto-created)
├── env.example.ps1       # config template -> copy to env.ps1
├── env.ps1               # your config: base url, token, headers, certs (gitignored)
└── token.txt             # your bearer token (gitignored; optional)
```

## Configuration — `env.ps1`

Copy the template and edit; `lib\common.ps1` dot-sources it automatically:

```powershell
Copy-Item tests\curl\env.example.ps1 tests\curl\env.ps1
```

It centralizes everything in one place:

| Setting | Purpose |
|---------|---------|
| `$Base` | API base URL (default `http://localhost:5185`) |
| `$Token` | Bearer token (or use `$env:PETSHOP_TOKEN` / `token.txt`) |
| `$CategoryId` | An existing category id used by the create/update bodies (default `1`) |
| `$ExtraHeaders` | Hashtable of headers sent on **every** request, e.g. `@{ 'X-Api-Version' = '1' }` |
| `$ClientCertPem` | mTLS client certificate (PEM) → `curl --cert` |
| `$ClientKeyPem` | Private key (PEM) if separate from the cert → `curl --key` |
| `$CaCertPem` | CA bundle to verify the server (PEM) → `curl --cacert` |
| `$Insecure` | `$true` to skip TLS verification (dev self-signed) → `curl -k` |

`$env:BASE` and `$env:PETSHOP_TOKEN` override the file when set (handy for CI). Headers and
cert options are applied to every request — authenticated or not — via `curl.exe`.

## Prerequisites

1. **The API is running** and reachable at `$env:BASE` (default `http://localhost:5185`):
   ```powershell
   dotnet run --project src\PetShop.Api
   ```
2. **An existing category.** There is no seed step and no category-creation endpoint, so the
   target database must already contain a category. Set `$CategoryId` in `env.ps1` to its id
   (default `1`); the create/update request bodies use it.
3. **A bearer token.** The harness does not mint one — the API validates an externally-issued
   RS256 JWT, which `curl` cannot sign. Obtain a token however your environment issues them,
   then provide it via either:
   - environment variable `PETSHOP_TOKEN`, or
   - a `tests\curl\token.txt` file (contents = the raw token).

   The token must carry an `email` claim (pets are owned per-caller) and the `Admin` role
   (for the delete case). See `Jwt` validation in `src\PetShop.Api\Program.cs`.

## Running

Needs `curl.exe` (built into Windows 10 1803+) and PowerShell — `pwsh` (PowerShell 7) or
`powershell` (Windows PowerShell 5.1).

```powershell
$env:BASE = 'http://localhost:5185'
$env:PETSHOP_TOKEN = 'eyJ...'           # or create tests\curl\token.txt

# all use cases
pwsh -ExecutionPolicy Bypass -File tests\curl\run.ps1

# selected case(s)
pwsh -File tests\curl\run.ps1 cases\create-pet.ps1 cases\get-pet.ps1

# a single case directly
pwsh -ExecutionPolicy Bypass -File tests\curl\cases\update-pet.ps1
```

> In Windows PowerShell 5.1, `curl` is an alias for `Invoke-WebRequest`; the harness
> resolves the real `curl.exe` explicitly, so you don't need to.

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
| `mtls-handshake` | With `$ClientCertPem` set over HTTPS: TLS handshake succeeds and the cert is accepted (skips if mTLS isn't configured) |

Each case is **self-contained** — those needing an existing pet create one first (via the
`New-Pet` helper), so you can run any single case in isolation and in any order.

## How a case works

```powershell
Invoke-Api create-pet 201 @Auth @Json -X POST "$Base/api/pets" -d "@$(Join-Path $Fix 'create-pet.json')"
Expect-Json create-pet { param($r) $r.success -and $r.data.name -eq 'Rex' -and $r.data.id -gt 0 } 'success + Rex + id'
```

`Invoke-Api` sends the request and asserts the HTTP status, capturing the body to
`responses\<name>.json`. `Expect-Json` then asserts on that body (and fails if it isn't
valid JSON). Each case exits non-zero if any check fails, so `run.ps1` — and CI — can gate
on it.

## Notes

- **State accumulates.** Cases create pets but only `delete-pet` cleans up, so re-runs leave
  extra "Rex" rows. Assertions use "contains", not exact counts, so that's harmless.
- **Line endings.** `.gitattributes` pins `*.ps1` to CRLF so the scripts stay Windows-native.
- **Responses** land in `responses\` (gitignored) — handy for debugging a failed assertion.
