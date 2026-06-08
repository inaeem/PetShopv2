# Shared config + helpers for the PowerShell curl use-case scripts.
# Dot-sourced by every cases\*.ps1 and used by run.ps1 — not run on its own.
#
# Uses curl.exe for the HTTP calls (ships with Windows 10 1803+) and PowerShell's
# native ConvertFrom-Json for response validation, so NO bash and NO jq are needed.

$ErrorActionPreference = 'Stop'

# Harness root from THIS file's location (lib\ -> root), so cases work standalone or via run.ps1.
$Root = Split-Path -Parent $PSScriptRoot
$Fix  = Join-Path $Root 'fixtures'
$Out  = Join-Path $Root 'responses'
New-Item -ItemType Directory -Force -Path $Out | Out-Null

# --- defaults (overridden by env.ps1, then by environment variables) ---
$Base          = 'http://localhost:5185'   # API base URL
$Token         = ''                         # bearer token
$ExtraHeaders  = @{}                         # extra headers sent on every request
$ClientCertPem = ''                          # mutual-TLS client cert (PEM)
$ClientKeyPem  = ''                          # private key (PEM) if separate from the cert
$CaCertPem     = ''                          # CA bundle to verify the server (PEM)
$Insecure      = $false                      # skip TLS verification (dev self-signed)
$CategoryId    = 1                           # existing category id used by create/update bodies

# Central config file (gitignored). Copy env.example.ps1 -> env.ps1 and edit.
$envFile = Join-Path $Root 'env.ps1'
if (Test-Path $envFile) { . $envFile }

# Environment variables win over the file (handy for CI / one-off overrides).
if ($env:BASE)           { $Base  = $env:BASE }
if ($env:PETSHOP_TOKEN)  { $Token = $env:PETSHOP_TOKEN }

# Resolve the real curl binary. In Windows PowerShell 5.1 `curl` is an ALIAS for
# Invoke-WebRequest, so filter to an Application and prefer curl.exe.
$Curl = (Get-Command curl.exe -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1).Source
if (-not $Curl) {
  $Curl = (Get-Command curl -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1).Source
}
if (-not $Curl) { Write-Error 'curl not found (needs Windows 10 1803+ or curl on PATH).'; exit 2 }

# Token fallback: env.ps1 / env var didn't set it -> try token.txt.
if (-not $Token) {
  $tokenFile = Join-Path $Root 'token.txt'
  if (Test-Path $tokenFile) { $Token = (Get-Content -Raw $tokenFile).Trim() }
}
if (-not $Token) { Write-Error 'No token. Set it in env.ps1, $env:PETSHOP_TOKEN, or tests\curl\token.txt'; exit 2 }

# --- build the curl args sent on EVERY request: extra headers + TLS/cert options ---
$CommonArgs = @()
foreach ($name in $ExtraHeaders.Keys) {
  $CommonArgs += @('-H', "${name}: $($ExtraHeaders[$name])")
}
if ($ClientCertPem) {
  if (-not (Test-Path $ClientCertPem)) { Write-Error "ClientCertPem not found: $ClientCertPem"; exit 2 }
  $CommonArgs += @('--cert', $ClientCertPem, '--cert-type', 'PEM')
  if ($ClientKeyPem) {
    if (-not (Test-Path $ClientKeyPem)) { Write-Error "ClientKeyPem not found: $ClientKeyPem"; exit 2 }
    $CommonArgs += @('--key', $ClientKeyPem, '--key-type', 'PEM')
  }
}
if ($CaCertPem) {
  if (-not (Test-Path $CaCertPem)) { Write-Error "CaCertPem not found: $CaCertPem"; exit 2 }
  $CommonArgs += @('--cacert', $CaCertPem)
}
if ($Insecure) { $CommonArgs += '-k' }

$Auth = @('-H', "Authorization: Bearer $Token")
$Json = @('-H', 'Content-Type: application/json')

$script:Fails = 0
function Pass($m) { Write-Host "  [PASS] $m" -ForegroundColor Green }
function Fail($m) { Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:Fails++ }

# Invoke-Api <name> <expected-status> <curl args...>
# Prepends the shared headers/cert options, captures the body to responses\<name>.json,
# and asserts the HTTP status code.
function Invoke-Api {
  param(
    [string]$Name,
    [int]$Expect,
    [Parameter(ValueFromRemainingArguments = $true)]$CurlArgs
  )
  $file = Join-Path $Out "$Name.json"
  $code = & $Curl -s -o $file -w '%{http_code}' @CommonArgs @CurlArgs
  if ("$code" -eq "$Expect") { Pass "$Name -> $code" }
  else { Fail "$Name expected $Expect, got $code (responses\$Name.json)" }
}

# Expect-Json <name> <predicate {param($r) ...}> <description>
# Parses responses\<name>.json and asserts the predicate. Also fails on malformed
# JSON — catching "returned 200 but the body wasn't the envelope".
function Expect-Json {
  param([string]$Name, [scriptblock]$Predicate, [string]$Desc)
  $file = Join-Path $Out "$Name.json"
  try {
    $r = Get-Content -Raw $file | ConvertFrom-Json
    if (& $Predicate $r) { Pass "$Name body OK: $Desc" }
    else { Fail "$Name body FAIL: $Desc (responses\$Name.json)" }
  } catch {
    Fail "$Name body not valid JSON: $Desc (responses\$Name.json)"
  }
}

# New-RequestBody <fixture-name> [@{ overrides }]
# Loads fixtures\<name>.json, applies overrides, and writes a request body to
# responses\_req-<name>.json (no BOM), returning its path. Used to inject the
# configured $CategoryId so the harness doesn't depend on a seeded category id.
function New-RequestBody {
  param([string]$Fixture, [hashtable]$Overrides = @{})
  $obj = Get-Content -Raw (Join-Path $Fix "$Fixture.json") | ConvertFrom-Json
  foreach ($k in $Overrides.Keys) {
    $obj | Add-Member -NotePropertyName $k -NotePropertyValue $Overrides[$k] -Force
  }
  $path = Join-Path $Out "_req-$Fixture.json"
  [System.IO.File]::WriteAllText($path, ($obj | ConvertTo-Json -Compress))
  $path
}

# Creates a pet (using $CategoryId) and returns its new id — for cases that need a
# pre-existing pet (get / update / delete / search) so they stay self-contained.
function New-Pet {
  $file = Join-Path $Out '_seed-pet.json'
  $body = New-RequestBody 'create-pet' @{ categoryId = $CategoryId }
  & $Curl -s -o $file @CommonArgs @Auth @Json -X POST "$Base/api/pets" -d "@$body" | Out-Null
  (Get-Content -Raw $file | ConvertFrom-Json).data.id
}

# Every case calls this last; exits with a code run.ps1 can tally.
function Complete-Case {
  if ($script:Fails -ne 0) { Write-Host "  -> $script:Fails check(s) failed"; exit 1 }
  exit 0
}
