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
- [Static web portal](#static-web-portal)
- [Logging & diagnostics](#logging--diagnostics)
- [Database & migrations](#database--migrations)
- [Testing](#testing)
- [Deployment (Windows / IIS)](#deployment-windows--iis)

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

Default seeded admin (Development/QA): **`admin` / `Admin#12345`** (change via
`Database:AdminPassword`).

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

| | Migrate on startup | Seed admin | Log level | Layer tracing | Swagger |
|---|---|---|---|---|---|
| Development | on | on | Debug | on | on |
| QA | off | on | Debug | on | on |
| Production | off | off | Information | off | **off** |

**Secrets** never go in the JSON files. Override placeholders with environment
variables (`__` separates config sections):

```bash
export ConnectionStrings__PetShopDb="Server=...;Database=PetShop;User Id=...;Password=..."
export Jwt__Key="a-long-random-secret-of-at-least-32-characters"
```

In dev use user-secrets: `dotnet user-secrets set "Jwt:Key" "<dev-secret>" --project src/PetShop.Api`.

---

## Authentication

1. `POST /api/auth/login` with `{ "username": "admin", "password": "Admin#12345" }`.
2. Copy `data.accessToken` from the response.
3. Send `Authorization: Bearer <token>` (or click **Authorize** in Swagger).

Passwords are hashed (PBKDF2). Auth/authorization failures (missing/invalid/expired
token → `401`; wrong role → `403`) return the standard envelope; an expired token
also sets a `Token-Expired: true` header. See `PetShop.Api.http` for ready-to-run
requests.

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

**Brand-new / empty database:**

```bash
dotnet ef database update --project src/PetShop.Data --startup-project src/PetShop.Api
```

**Existing database with the matching schema (don't recreate tables):** baseline it
once so EF marks `InitialCreate` as already applied, then update:

```bash
sqlcmd -S <server> -d PetShop -i database/04_baseline_existing_database.sql
dotnet ef database update --project src/PetShop.Data --startup-project src/PetShop.Api
```

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

- `GET /api/pets` returns **200** (anonymous) — app + DB are up.
- `POST /api/auth/login` with a provisioned user returns a token.
- Swagger is available in Development/QA, **not** Production.

**Scale-out notes** — migrate from one place only (never on startup in QA/Prod);
JWTs are stateless so the API scales horizontally with the **same `Jwt:Key`** on
every instance; ensure the service account can write the `logs\` folder.
