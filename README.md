# PetShop API

A layered ASP.NET Core 8 Web API for a pet shop. It demonstrates a clean, simple
architecture with payload validation, JWT authentication, filters, structured
logging, a service layer, and an EF Core data layer (database-first) that can also
invoke SQL Server stored procedures directly. Swagger and a small static web portal
are included.

- **Hosting:** Windows / IIS. **No Docker.**
- **Automation:** bash scripts in `scripts/` (no CI server required).
- **API style:** code-first — the OpenAPI spec is generated from the code.

---

## Table of contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Build & compile](#build--compile)
- [Run](#run)
- [Debug](#debug)
- [Scripts](#scripts)
- [Configuration & environments](#configuration--environments)
- [Authentication](#authentication)
- [API documentation (Swagger)](#api-documentation-swagger)
- [External pet sync](#external-pet-sync)
- [Static web portal](#static-web-portal)
- [Logging & diagnostics](#logging--diagnostics)
- [Database & migrations](#database--migrations)
- [Testing](#testing)
- [Deployment (Windows / IIS)](#deployment-windows--iis)
- [Using PetShop as a base project](#using-petshop-as-a-base-project)
- [Troubleshooting](#troubleshooting)

---

## Architecture

Four projects, dependencies flowing one way (`Api → Service → Data → Domain`):

```
PetShop.sln
├── src/
│   ├── PetShop.Domain    — entities & enums (no dependencies)
│   ├── PetShop.Data      — DbContext, EF configs, repositories, UnitOfWork,
│   │                       migrations, stored-procedure calls, layer tracer
│   ├── PetShop.Service   — DTOs, FluentValidation, services, JWT/security, mapping
│   └── PetShop.Api       — controllers, filters, middleware, Program.cs, wwwroot
├── tests/
│   └── PetShop.Tests.E2E — end-to-end tests against a real SQL Server
├── database/             — raw SQL (schema, stored proc, seed, baseline)
└── scripts/              — build / publish / migrate / swagger / run helpers
```

**Cross-cutting concerns** are handled in the API layer:

- **Validation** — FluentValidation validators run via a global `ValidationFilter`;
  model-binding errors are reshaped to the same envelope.
- **Authentication** — JWT bearer; auth/authz failures return the uniform envelope
  (`401`/`403`) instead of empty bodies.
- **Filters** — global action filters (request logging, validation) + an exception
  filter for domain errors; a last-resort exception middleware for everything else.
- **Logging** — Serilog (console + rolling file), a correlation id per request, and
  configurable per-layer entry/exit tracing.

**Response envelope** — every endpoint returns:

```json
{ "success": true, "message": null, "data": { }, "errors": null }
```

**Request pipeline** (outer → inner): correlation id → exception handler →
request logging → Swagger (Dev/QA) → static files → CORS → authentication →
authorization → controllers. A failure is handled at the first matching stage, so
the layer that reports it tells you where it failed.

---

## Prerequisites

- **.NET 8 SDK** (`dotnet --version` → `8.x`).
- **SQL Server** reachable from where the app runs (LocalDB is fine for local dev).
- For migrations/spec export, the local tools restore automatically
  (`.config/dotnet-tools.json` pins `dotnet-ef` and the Swagger CLI).

---

## Build & compile

```bash
dotnet restore                       # restore NuGet packages
dotnet build -c Release              # compile (use -c Debug for a debug build)
```

Or use the script (restore + compile + generate OpenAPI spec + tests):

```bash
./scripts/build.sh                   # Release; writes artifacts/swagger.json
RUN_TESTS=false ./scripts/build.sh   # skip e2e tests (they need SQL Server)
CONFIG=Debug ./scripts/build.sh      # debug build
```

Publish (compile + produce a deployable folder):

```bash
dotnet publish src/PetShop.Api -c Release -o ./publish
```

---

## Run

```bash
dotnet run --project src/PetShop.Api                       # Development
./scripts/run.sh QA                                        # QA (or Development / Production)
ASPNETCORE_ENVIRONMENT=Production dotnet run --project src/PetShop.Api
```

Local URLs by launch profile (`src/PetShop.Api/Properties/launchSettings.json`):

| Profile | URL |
|---------|-----|
| Development | `https://localhost:7185` (Swagger at `/swagger`) |
| QA | `https://localhost:7186` |
| Production | `http://localhost:5187` |

The API does not issue tokens — every endpoint requires an externally-obtained JWT
in the `Authorization` header (see [Authentication](#authentication)).

---

## Debug

- **Visual Studio:** open `PetShop.sln`, set **PetShop.Api** as the startup project,
  pick the **Development** launch profile, press **F5**. Breakpoints work across all
  layers (they're all in the same solution).
- **VS Code:** install the C# Dev Kit, open the folder, and run/debug `PetShop.Api`
  (or `dotnet run --project src/PetShop.Api` and attach).
- **Rider:** open the solution and use the generated run/debug configuration.

Helpful while debugging:

- Run in **Development** — Serilog logs at `Debug` and per-layer tracing is on, so
  you can follow a request across the service/data layers (see
  [Logging & diagnostics](#logging--diagnostics)).
- Each log line carries a **correlation id**; filter by it to see everything for one
  request, including where it failed.
- To see the SQL EF emits, set
  `Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command` to
  `Information`.
- To debug a deployed instance, attach to the IIS worker process (`w3csvc`/`dotnet`).

---

## Scripts

Bash scripts (Linux/macOS or Git Bash on Windows); they only need the .NET SDK on
`PATH`.

| Script | Does |
|--------|------|
| `scripts/release.sh` | **build + test + publish in one go** |
| `scripts/build.sh` | restore + compile + generate `artifacts/swagger.json` + test |
| `scripts/publish.sh` | publish the API + migration artifacts + `swagger.json`, zipped to `artifacts/petshop-api.zip` |
| `scripts/migrate.sh` | apply migrations to a reachable DB (`PETSHOP_CONNECTION=...`) |
| `scripts/swagger.sh` | export the OpenAPI document to `artifacts/swagger.json` (no DB needed) |
| `scripts/run.sh` | run the API locally for an environment (`scripts/run.sh QA`) |

Typical no-CI release: `scripts/release.sh` → copy `artifacts/petshop-api.zip` to the
server → run the DB step → deploy to IIS.

---

## Configuration & environments

Config layers: `appsettings.json` ← `appsettings.{Environment}.json` ← environment
variables ← command line. The environment is `ASPNETCORE_ENVIRONMENT`
(`Development`, `QA`, `Production`).

| | Migrate on startup | Log level | Layer tracing | Swagger |
|---|---|---|---|---|
| Development | on | Debug | on | on |
| QA | off | Debug | on | on |
| Production | off | Information | off | **off** |

**Secrets** never go in the JSON files. Override placeholders with environment
variables (`__` separates config sections):

```bash
export ConnectionStrings__PetShopDb="Server=...;Database=PetShop;User Id=...;Password=..."
export Jwt__Key="a-long-random-secret-of-at-least-32-characters"
```

In dev use user-secrets: `dotnet user-secrets set "Jwt:Key" "<dev-secret>" --project src/PetShop.Api`.

### Layering principle — define once, override the delta

`appsettings.json` is the **single base** that holds every section with safe defaults
(`Jwt`, `PetSync`, `Database`, `Diagnostics`, the full `Serilog` sink setup, …). Each
`appsettings.{Environment}.json` then overrides **only what differs** for that
environment — never a full copy. So:

- A new setting (e.g. another `PetSync` field) goes in **base only**; every
  environment inherits it automatically.
- An environment file should contain just its deltas (connection string, log level,
  tracing toggle, `ApplyMigrationsOnStartup`). If a value matches base, leave it out.
- Secrets are never in any JSON file — they come from env vars / user-secrets (above),
  which sit *above* the env file in the layer order and win.

### Connection-string pattern

One key, `ConnectionStrings:PetShopDb`, redefined per environment — the server and
auth differ, so this is an intentional override, not duplication:

| Env | Server / auth | Encryption |
|-----|---------------|------------|
| Base / Development | `localhost` / `(localdb)\MSSQLLocalDB`, Trusted (Windows) auth | `TrustServerCertificate=True` (local dev) |
| QA | host + SQL login; password is `__SET_VIA_ENV_OR_SECRET__` | `TrustServerCertificate=True` |
| Production | host + SQL login; password is `__SET_VIA_ENV_OR_SECRET__` | `Encrypt=True;TrustServerCertificate=False` (validate the cert) |

The `__SET_VIA_ENV_OR_SECRET__` placeholders are replaced at runtime by
`ConnectionStrings__PetShopDb` (whole string) or just the password via your secret
store — the JSON only documents the shape.

### Serilog pattern

The **sinks are configured once in base** (`Serilog:WriteTo` → Console + rolling File
at `logs/petshop-*.log`, 7-day retention, `CorrelationId`/`SourceContext` in the
template, `Enrich:FromLogContext`). Environment files override **only `MinimumLevel`**:

| Env | Default level | `Microsoft.AspNetCore` | EF `Database.Command` |
|-----|---------------|------------------------|-----------------------|
| Base | Information | Warning | Warning |
| Development | Debug | Information | (inherits Warning) |
| QA | Debug | Information | Information |
| Production | Information | Warning | Warning |

To see the SQL EF emits, lower `Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore.Database.Command`
to `Information` (already the case in QA). Serilog reads this config in `Program.cs`
via `ReadFrom.Configuration(...)`, so no redeploy is needed to change levels — just
the env file or an env-var override.

---

## Authentication

The API **does not issue tokens** — there is no login or registration and no user
store. Clients obtain a JWT elsewhere and send it on **every** request:

1. Acquire a JWT from your token issuer (signed with the shared `Jwt:Key`, with the
   expected issuer/audience, and any role claims the endpoints need).
2. Send `Authorization: Bearer <token>` (or click **Authorize** in Swagger).

The API **validates** the token (signature against `Jwt:Key`, issuer, audience,
lifetime) — see the JWT-bearer setup in `Program.cs`. All endpoints require a valid
token; `Delete` additionally requires the `Admin` role (from the token's role claim).
Auth/authorization failures (missing/invalid/expired token → `401`; wrong role →
`403`) return the standard envelope; an expired token also sets a `Token-Expired: true`
header. See `PetShop.Api.http` for ready-to-run requests.

---

## API documentation (Swagger)

Code-first: the OpenAPI document is generated from the code (Swashbuckle), not
hand-written. Exposed in **Development and QA only — never Production**:

- **Swagger UI:** `/swagger`
- **Raw OpenAPI JSON:** `/swagger/v1/swagger.json`

`scripts/build.sh` and `scripts/publish.sh` also emit `artifacts/swagger.json`
(publish bundles it into the zip); `scripts/swagger.sh` regenerates it on demand
without a database.

> **⚠️ Restrict Swagger in QA.** It exposes the full API surface — keep QA on an
> internal network/VPN and restrict the `/swagger` path at the host (IIS IP
> restrictions, firewall allow-list, or auth in front).

---

## External pet sync

Creating a pet runs a small two-step flow: the pet is **persisted locally first**,
then replicated to an external pet service. The remote call is **best-effort** — the
pet is already saved, so a remote failure is logged and the create still succeeds (it
never rolls back or throws). The logic lives in the service layer
(`PetService.CreateAsync` → `IPetSyncClient`); the client is a typed `HttpClient`
(`PetShop.Service/External/PetSyncClient.cs`) that POSTs the pet and retries
transient failures (5xx / 408 / 429 / network) with a short backoff.

**Authorization** — the external service does authorization only, and this app
authenticates with its **own service token** (not the caller's JWT). A fixed token
from config (`PetSync:ServiceToken`) is sent on every outbound call as
`Authorization: Bearer <token>`. (This is separate from the inbound JWT that
authorizes the caller's request to `POST /api/pets`.)

**Wire contract** — `POST {BaseUrl}{CreatePetPath}`, `Authorization: Bearer <service-token>`:

```jsonc
// request — pet wrapped in an envelope
{ "pet": { "localId": 1, "name": "Rex", "breed": "Lab", "price": 250.0,
           "ageMonths": 8, "categoryId": 2, "status": "Available" } }

// success response — remote id nested under "data" (optional; logged, not stored)
{ "data": { "id": "remote-abc-123" } }
```

The wrapper keys (`pet`, `data`) and field names are pinned with `[JsonPropertyName]`
in `PetSyncClient.cs` — change the records there if the contract differs.

It is **disabled by default**. Configure it via the `PetSync` section:

```jsonc
"PetSync": {
  "Enabled": false,                 // turn on once you have a real endpoint
  "BaseUrl": "https://pets.example.com",
  "CreatePetPath": "/pets",         // POSTed here, relative to BaseUrl
  "ServiceToken": "",               // secret — supply via env var / user-secrets
  "TimeoutSeconds": 10,
  "MaxRetries": 2,                  // extra attempts after the first
  "RetryBaseDelayMs": 200           // backoff = this × attempt number
}
```

The **service token is a secret** — never put it in committed JSON. Supply it (and
enable sync) via env vars / user-secrets:

```bash
export PetSync__Enabled=true
export PetSync__ServiceToken="<your-service-token>"
```

> No schema change: the remote id (if the service returns one) is logged, not stored.
> To persist sync state instead, add columns to `dbo.Pets` and a migration, then record
> the result from `PetSyncResult` after the call.

---

## Static web portal

`src/PetShop.Api/wwwroot` (`index.html`, `css`, `js`) is a small portal served by
the API at the site root in every environment (`UseDefaultFiles`/`UseStaticFiles`).
It is **not** a separate app — run the API and open the root URL (e.g.
`https://localhost:7185/`). It calls the API on the same origin, so opening
`index.html` from disk (`file://`) won't work.

---

## Logging & diagnostics

- **Serilog** → console + rolling file (`logs/petshop-*.log`, 7 days), configured in
  `appsettings*.json`. Each line shows the `CorrelationId` and `SourceContext`.
- **Correlation id** per request (honours an inbound `X-Correlation-ID`, else the
  trace id) is pushed onto every log line and echoed back in the response header.
- **Per-layer tracing** — configurable entry/exit `Debug` logs for the service and
  data layers via `Diagnostics:LayerTracing` (toggle at runtime; gated by the
  `Debug` level). A faulted call is flagged on its exit line with the exception type,
  so the deepest `FAULTED` line pinpoints the originating layer.

```jsonc
"Diagnostics": { "LayerTracing": { "Enabled": true, "Service": true, "Data": true, "IncludeArguments": false } }
```

---

## Database & migrations

EF Core records applied migrations in `__EFMigrationsHistory`, so a migration
**never runs twice** against the same database. The only special case is the first
run against a database that already has the schema.

**Migrations** (applied in order):

| Migration | Does |
|-----------|------|
| `20260101000000_InitialCreate` | Baseline schema — Categories, Pets. |
| `20260101000100_AddSearchPetsProcedure` | Creates `dbo.usp_SearchPets`. |
| `20260101000200_DropOrderTables` | Drops the legacy `Customers`, `Orders`, `OrderItems` tables (scope is now Pets-only). Guarded with `IF OBJECT_ID` checks, so it's a no-op on a database that never had them. |
| `20260101000300_DropUsersTable` | Drops the legacy `Users` table (the API no longer stores users — clients send an externally-issued JWT). Also `IF OBJECT_ID`-guarded, so it's a no-op where the table never existed. |

**Brand-new / empty database:**

```bash
dotnet ef database update --project src/PetShop.Data --startup-project src/PetShop.Api
```

All three run; `DropOrderTables` is a harmless no-op since `InitialCreate` no longer
creates those tables.

**Existing database with the matching schema (don't recreate tables):** baseline it
once so EF marks `InitialCreate` as already applied, then update:

```bash
sqlcmd -S <server> -d PetShop -i database/04_baseline_existing_database.sql \
       -v MigrationId="20260101000000_InitialCreate" ProductVersion="8.0.6"
dotnet ef database update --project src/PetShop.Data --startup-project src/PetShop.Api
```

`database update` then applies `AddSearchPetsProcedure`, `DropOrderTables` and
`DropUsersTable` — the latter two **remove the legacy Customers/Orders/OrderItems and
Users tables and their data**. Only baseline `InitialCreate` (so it isn't recreated);
do **not** baseline the `Drop*` migrations, or they won't run and the old tables will
linger.

> ⚠️ `DropOrderTables` and `DropUsersTable` are **destructive** — they delete all
> Customers/Orders/OrderItems and Users rows. Back those up first if they matter, and
> prefer reviewing the generated SQL (`dotnet ef migrations script --idempotent`)
> before applying in QA/Prod.

The baseline script is a reusable template — it takes the migration id and EF
version as `sqlcmd` `-v` variables (shown above are PetShop's values) and is
idempotent, so re-running it is harmless.

**Deploying migrations (QA/Prod):** startup migration is **off** there, so apply them
as a controlled step using the idempotent SQL script or the self-contained bundle
produced by `scripts/publish.sh`:

```bash
efbundle.exe --connection "Server=...;Database=PetShop;User Id=...;Password=..."
# or run migrate.sql via sqlcmd
```

`scripts/migrate.sh` applies migrations from a machine that can reach the DB
(`PETSHOP_CONNECTION=...`). The raw SQL scripts (schema, stored proc, seed) live in
`database/`.

---

## Testing

`tests/PetShop.Tests.E2E` runs the whole API in-process (`WebApplicationFactory`)
against a **real SQL Server**, covering auth, the response envelope, the
service/data layers, and the `usp_SearchPets` stored procedure. It creates a
uniquely-named throwaway database (migrated + seeded by app startup) and drops it
afterwards.

```bash
dotnet test
```

- **No Docker.** Defaults to SQL Server **LocalDB**.
- Point at any reachable SQL Server with `PETSHOP_TEST_CONNECTION` (the test DB name
  is appended automatically; the login needs create/drop-database rights).

---

## Deployment (Windows / IIS)

The same publish output is promoted across environments; only configuration changes.

**1. Prerequisites on the server** — install the **.NET 8 ASP.NET Core Hosting
Bundle** (adds the runtime + the IIS module). A self-contained publish needs no
runtime. A SQL Server login for the app.

**2. Produce the package**

```bash
./scripts/release.sh                 # framework-dependent
SELF_CONTAINED=true ./scripts/release.sh   # bundles the runtime (no .NET on server)
```

Output: `artifacts/petshop-api.zip` (`app/`, `efbundle.exe`, `migrate.sql`,
`swagger.json`, baseline script).

**3. Apply the database migration** (separate, one-time per release — see
[Database & migrations](#database--migrations)). Baseline an existing DB once, then
run `efbundle.exe` / `migrate.sql`.

**4. Deploy to IIS**

1. Install the Hosting Bundle, then `net stop was /y && net start w3svc`.
2. Copy `artifacts/app/` to e.g. `C:\inetpub\petshop-api`.
3. Create an **Application Pool** with **.NET CLR Version = No Managed Code**.
4. Create a **Website/Application** pointing at the folder, using that app pool.
5. Provide config via the generated `web.config` `<environmentVariables>` (or app-pool env):

   ```xml
   <aspNetCore processPath="dotnet" arguments=".\PetShop.Api.dll" hostingModel="OutOfProcess">
     <environmentVariables>
       <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Production" />
       <environmentVariable name="ConnectionStrings__PetShopDb" value="Server=...;Database=PetShop;User Id=...;Password=...;Encrypt=True;" />
       <environmentVariable name="Jwt__Key" value="a-long-random-secret-of-at-least-32-characters" />
     </environmentVariables>
   </aspNetCore>
   ```

   For a self-contained publish use `processPath=".\PetShop.Api.exe"` and `arguments=""`.
6. Give the app-pool identity write access to the app's `logs\` folder and access to SQL Server.

**5. Verify**

- `GET /api/pets` **without** a token returns **401** — the API is up and enforcing auth.
- `GET /api/pets` **with** a valid bearer token returns **200** — app + DB are up.
- Swagger is available in Development/QA, **not** Production.

**Scale-out notes** — migrate from one place only (never on startup in QA/Prod);
JWTs are stateless so the API scales horizontally with the **same `Jwt:Key`** on
every instance; ensure the service account can write the `logs\` folder.

---

## Using PetShop as a base project

PetShop is intentionally a clean, opinionated template: the **layering, cross-cutting
concerns, scripts, config, and deployment story are reusable**; only the *pet-shop
domain* (entities, validators, controllers, SQL) is throwaway. Porting it into a new
project is mostly a rename pass plus swapping the domain.

### What to keep vs. replace

| Keep (the scaffolding) | Replace (the domain) |
|---|---|
| The 4-layer structure (`Domain → Data → Service → Api`) and project references | Entities in `PetShop.Domain` (`Pet`, `Category`, …) |
| Response envelope, filters, exception middleware, correlation id | `IEntityTypeConfiguration<T>` configs + repositories |
| JWT bearer **validation** (shared-key), `ValidationFilter` | DTOs, FluentValidation validators, services |
| Serilog setup, per-layer tracing, `LayerTracer` | Controllers under `PetShop.Api/Controllers` |
| `scripts/`, `appsettings*.json` layout, IIS/`web.config` flow | `database/*.sql`, the `usp_SearchPets` proc + `PetSearchResult` |
| EF Core + migrations plumbing, `UnitOfWork` | The migrations themselves (regenerate for your schema) |
| The `wwwroot` portal shell | The portal's pet-shop content (`index.html`, `js`) |

> The commands below are **PowerShell** (Windows). Set your new project name once and
> reuse it; every step is driven by the `$New` variable so there is **no leftover
> `PetShop` reference** when you finish.

```powershell
$Old = 'PetShop'
$New = 'Contoso.Crm'    # <-- your project name (used as the namespace/assembly prefix)
```

> Use a valid .NET identifier prefix (letters/digits/dots). `Contoso.Crm` →
> projects `Contoso.Crm.Domain`, `Contoso.Crm.Api`, namespace `Contoso.Crm.*`.

**1. Copy the code without git history.** Start from a clean tree so you don't inherit
PetShop's history or build output:

```powershell
git clone <petshop-repo> MyApp; Set-Location MyApp
Remove-Item -Recurse -Force .git
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
git init
```

**2. Rename solution, projects, folders, files, and namespaces in one pass.** The
namespaces equal the project/folder names, so renaming the items and replacing the
`PetShop` string everywhere is all it takes:

```powershell
# (a) rename every file & folder whose NAME contains the old prefix — deepest paths first
Get-ChildItem -Recurse -Force |
  Where-Object { $_.FullName -notmatch '\\\.git\\' -and $_.Name -like "*$Old*" } |
  Sort-Object { $_.FullName.Length } -Descending |
  ForEach-Object { Rename-Item -LiteralPath $_.FullName -NewName ($_.Name -replace [regex]::Escape($Old), $New) }

# (b) replace the string INSIDE every text file (skips .git/bin/obj and binaries)
$ext = '\.(cs|csproj|sln|json|jsonc|config|props|targets|sh|ps1|http|md|html|css|js|sql|xml|editorconfig|gitignore|gitattributes)$'
Get-ChildItem -Recurse -File -Force |
  Where-Object { $_.FullName -notmatch '\\(\.git|bin|obj)\\' -and $_.Name -match $ext } |
  ForEach-Object {
    $c = Get-Content -Raw -LiteralPath $_.FullName
    [System.IO.File]::WriteAllText($_.FullName, ($c -replace [regex]::Escape($Old), $New))
  }
```

This fixes namespaces, `using` statements, `.sln` project paths, the DbContext class
name, `ProjectReference` paths, and the `scripts/` that reference `src/PetShop.Api`.
Verify nothing is left, then open the renamed solution:

```powershell
Get-ChildItem -Recurse -File -Force |
  Where-Object { $_.FullName -notmatch '\\(\.git|bin|obj)\\' } |
  Select-String -Pattern $Old        # expect: no output
dotnet build -c Release              # confirm all five projects compile
```

**3. Update config keys and identifiers** the rename doesn't infer (these use the word
"PetShop" as data, e.g. the literal DB name — adjust to your own):

| Thing | Where | Old value → set to |
|---|---|---|
| Connection-string key | `appsettings*.json`, env `ConnectionStrings__PetShopDb` | `PetShopDb` → your key |
| Database name | connection strings | `Database=PetShop` → your DB |
| Log file name | `appsettings.json` Serilog sink | `logs/petshop-.log` |
| Release artifact | `scripts/publish.sh` | `petshop-api.zip` |
| Migrate/test env vars | `scripts/migrate.sh`, tests | `PETSHOP_CONNECTION`, `PETSHOP_TEST_CONNECTION` |
| Launch URLs/ports (optional) | `src/*.Api/Properties/launchSettings.json` | `7185`/`7186`/`5187` |

**4. Replace the domain models.** Delete the pet-shop entities and add yours, then ripple
outward — `Domain` entities → `Data` configs/repositories → `Service` DTOs/validators →
`Api` controllers. Remove what you don't use (e.g. `StoredProcedures/PetSearchResult.cs`
and its `usp_SearchPets` config) and update the `wwwroot` portal content. If your model is
large, **reverse-engineer it from your existing database** instead of hand-writing it
(see step 5).

**5. Create migrations from your existing database.** Your DB already exists with data, so
the goal is: get a model that matches it → produce a baseline `InitialCreate` → tell EF
the baseline is **already applied** (so it never re-creates your populated tables).

> **Prerequisite — the `*.Api` startup project must reference `Microsoft.EntityFrameworkCore.Design`.**
> Every `dotnet ef` command below (scaffold, `migrations add`, `database update`) inspects the
> `--startup-project` for this package. `*.Data` references it with `PrivateAssets=all`, which
> intentionally stops it flowing to dependents, so the Api needs its own reference or you'll get
> *"startup project … doesn't reference Microsoft.EntityFrameworkCore.Design."* Add to `src/$New.Api/$New.Api.csproj`:
>
> ```xml
> <PackageReference Include="Microsoft.EntityFrameworkCore.Design">
>   <PrivateAssets>all</PrivateAssets>
>   <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
> </PackageReference>
> ```

```powershell
$conn = 'Server=<srv>;Database=<YourDb>;Trusted_Connection=True;TrustServerCertificate=True'

# Point EVERY command at the legacy DB. Scaffold (5a) takes the connection string as an
# argument, but `migrations add` / `database update` read it from the Api's configuration
# (AddDataLayer → ConnectionStrings:PetShopDb). Export it so they don't silently fall back
# to the LocalDB string in appsettings.Development.json.
$env:ConnectionStrings__PetShopDb = $conn
$env:ASPNETCORE_ENVIRONMENT       = 'Development'

# 5a. (optional) reverse-engineer entities from the live DB if you didn't hand-write them.
#     --no-onconfiguring keeps the connection string OUT of the generated code (no hard-coded
#     secret) and preserves the DbContextOptions constructor that AddDbContext needs.
dotnet ef dbcontext scaffold "$conn" Microsoft.EntityFrameworkCore.SqlServer `
  --project src/$New.Data --startup-project src/$New.Api --output-dir Models --force --no-onconfiguring

# 5b. CONSOLIDATE to a SINGLE DbContext. Scaffold emits its own context (named after the DB);
#     the template already ships one. Keep exactly one — delete the other and make sure
#     DependencyInjection.cs registers it. Two contexts → "More than one DbContext was found.
#     Specify --context" on the next command.

# 5c. remove PetShop's migrations AND its snapshot, then generate a baseline for YOUR model
Remove-Item src/$New.Data/Migrations/*.cs -Force
dotnet ef migrations add InitialCreate --project src/$New.Data --startup-project src/$New.Api

# 5d. baseline the EXISTING database: record InitialCreate as applied WITHOUT running its Up()
$mig = (Get-ChildItem "src/$New.Data/Migrations/*_InitialCreate.cs").BaseName
sqlcmd -S <srv> -d <YourDb> -i database/04_baseline_existing_database.sql `
       -v MigrationId="$mig" ProductVersion="8.0.6"
```

> `database/04_baseline_existing_database.sql` is a reusable, idempotent template —
> it creates `__EFMigrationsHistory` if missing and records your `InitialCreate` as
> applied, taking the migration id and EF version as `sqlcmd` `-v` variables so
> there is nothing project-specific to edit.

#### Guard the legacy database against accidental create/drop

The generated `InitialCreate.Up()` is full of `CREATE TABLE` and its `Down()` is full of
`DROP TABLE`. Against a populated legacy DB that's a footgun, so harden it — defence in depth:

**1. Make the baseline migration inert.** Empty its `Up()` and make `Down()` refuse, so even an
accidental `database update` / rollback can't touch the legacy tables:

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Baseline of a pre-existing legacy database — the schema already exists,
        // so this intentionally does NOTHING. (For dev/test DBs that build from
        // migrations, guard each table with IF OBJECT_ID(...) IS NULL instead of emptying.)
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "InitialCreate is a baseline of an existing database and cannot be rolled back.");
}
```

> ⚠️ **Leave `…ModelSnapshot.cs` untouched** — it must keep describing the *full* legacy schema.
> EF diffs your entities against the snapshot (not against `Up()`) to build the next migration,
> so a full snapshot + empty `Up()` is exactly what you want. If you blank the snapshot too, your
> next `migrations add` will emit `CREATE TABLE` for the entire schema — the disaster you just avoided.

**2. Never auto-migrate or `database update` shared environments.** Keep
`Database:ApplyMigrationsOnStartup = false` (QA/Prod default) and deploy schema changes as a
**reviewed, idempotent script** — already-applied migrations (your baselined `InitialCreate`) are
skipped, and a human reads the SQL first:

```powershell
dotnet ef migrations script --idempotent --output migrate.sql `
  --project src/$New.Data --startup-project src/$New.Api
```

**3. Take away the permission.** Give the app's runtime SQL login `db_datareader` +
`db_datawriter` + EXECUTE only — **no DDL**. Run migrations with a separate deploy-time login that
has DDL rights, used only in a gated pipeline. Then the database itself refuses an accidental
`CREATE`/`DROP`, whatever the code says.

After this, EF sees `InitialCreate` as applied. **Future** schema changes are normal — and a
genuine `DropTable` only ever appears in a migration *you* author for a change *you* are making:

```powershell
dotnet ef migrations add <ChangeName> --project src/$New.Data --startup-project src/$New.Api
dotnet ef database update         --project src/$New.Data --startup-project src/$New.Api
```

> A brand-new/empty DB instead of an existing one? Skip 5a/5d and the inert-baseline guard, and
> just run `dotnet ef database update` — see [Database & migrations](#database--migrations).
> Always review a generated migration before applying it to a DB with real rows.

**6. Rotate secrets.** Set a **new `Jwt:Key`** (≥32 chars) — it must match the key your
token issuer signs with — never reuse the sample value. Use env vars / user-secrets as
in [Configuration & environments](#configuration--environments).

**7. Build, test, verify.**

```powershell
dotnet build -c Release            # rename compiles everywhere
dotnet test                        # after updating the e2e tests for your routes
./scripts/build.sh                 # restore + compile + emit artifacts/swagger.json (Git Bash)
```

### Quick checklist

- [ ] All five projects renamed (folders, `.csproj`, `.sln`, namespaces); `Select-String PetShop` returns nothing
- [ ] Connection-string key, DB name, log path, artifact name, env vars updated to your project
- [ ] Domain models replaced (or scaffolded from the DB); unused stored-proc code removed
- [ ] `*.Api` startup project references `Microsoft.EntityFrameworkCore.Design` (needed by every `dotnet ef` command)
- [ ] `ConnectionStrings__PetShopDb` exported so `migrations add`/`database update` hit the legacy DB, not LocalDB
- [ ] Exactly one `DbContext` (scaffolded duplicate removed); registered in `DependencyInjection.cs`
- [ ] Migrations reset; fresh `InitialCreate` generated and **baselined** against the existing DB
- [ ] Legacy DB guarded: `InitialCreate.Up()` inert + `Down()` throws (snapshot left full); no auto-migrate; runtime login has no DDL
- [ ] New `Jwt:Key` (matching your token issuer); no sample secrets committed
- [ ] `dotnet build` + `dotnet test` green; `scripts/build.sh` produces `swagger.json`

---

## Troubleshooting

### "This project was downloaded from the web" / blocked files

Windows stamps files that came from the internet or another computer with the
**Mark of the Web** (a hidden `Zone.Identifier` stream). Visual Studio then warns
when you open the solution, and `.dll`/scripts may be blocked. It is a Windows file
attribute — nothing in the project — so the fix is to clear that mark:

```powershell
# Best: unblock the zip BEFORE extracting (extracted files stay clean)
Unblock-File .\petshop-api.zip          # or right-click the zip → Properties → Unblock
Expand-Archive .\petshop-api.zip

# Already extracted: unblock everything recursively
Get-ChildItem -Path . -Recurse | Unblock-File
```

Single file: right-click → **Properties** → tick **Unblock** → **Apply**. In Visual
Studio you can also manage the prompt under *Tools → Options → Trust Settings*.

**Avoid it entirely** by transferring the code via **`git clone`** or an internal
artifact/file share instead of a browser download or email attachment — those
sources don't apply the mark.
