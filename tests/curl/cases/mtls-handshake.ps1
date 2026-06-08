# Use case: when a client certificate is configured (env.ps1 $ClientCertPem), the mutual-TLS
# handshake completes and the certificate is accepted — i.e. the cert/key/CA wiring is valid.
#
# Note: the PetShop API authenticates with JWT, not client certs, and exposes no endpoint that
# echoes the presented certificate. So this proves the handshake SUCCEEDS with the cert in play
# (no TLS/cert-rejection failure), not that the server specifically required or inspected it.
# Point $Base at an mTLS-terminating endpoint (gateway / reverse proxy) for a stricter check.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[mtls-handshake]'

# Only meaningful with a client cert over HTTPS — otherwise skip (not fail).
if (-not $ClientCertPem) {
  Write-Host '  [SKIP] no $ClientCertPem set in env.ps1 — mTLS not configured'
  exit 0
}
if ($Base -notlike 'https:*') {
  Write-Host "  [SKIP] `$Base is not https ($Base) — mTLS needs TLS"
  exit 0
}

# Verbose request: curl writes the TLS handshake trace to stderr; the body to a file;
# the HTTP status to stdout. $CommonArgs already carries --cert/--key/--cacert.
$verboseFile = Join-Path $Out 'mtls.verbose.txt'
$bodyFile    = Join-Path $Out 'mtls.json'
$code = & $Curl -sv -o $bodyFile -w '%{http_code}' @CommonArgs @Auth "$Base/api/pets?page=1&pageSize=1" 2> $verboseFile
$exitCode = $LASTEXITCODE
$trace = Get-Content -Raw $verboseFile

# 1. No transport/TLS error (35 = handshake, 58 = client cert, 60 = server verify, ...).
if ($exitCode -eq 0) { Pass 'curl exit 0 (no TLS/transport failure)' }
else { Fail "curl exit $exitCode — TLS/transport failure (see responses\mtls.verbose.txt)" }

# 2. A TLS session was actually negotiated.
if ($trace -match 'SSL connection using|TLS\w* handshake|TLSv1') { Pass 'TLS session established' }
else { Fail 'no TLS handshake in verbose trace (responses\mtls.verbose.txt)' }

# 3. The certificate was not rejected by either side.
if ($trace -notmatch 'SSL certificate problem|alert (bad|unknown|certificate)|handshake failure') {
  Pass 'certificate accepted (no rejection in handshake)'
} else {
  Fail 'certificate rejected during handshake (responses\mtls.verbose.txt)'
}

# 4. The server returned an HTTP response over the mTLS connection.
if ($code -match '^\d{3}$') { Pass "HTTP response received over mTLS ($code)" }
else { Fail 'no HTTP response — handshake likely failed' }

Complete-Case
