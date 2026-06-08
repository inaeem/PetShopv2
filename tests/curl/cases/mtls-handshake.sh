#!/usr/bin/env bash
# Use case: when a client certificate is configured (env.sh CLIENT_CERT_PEM), the mutual-TLS
# handshake completes and the certificate is accepted — i.e. the cert/key/CA wiring is valid.
#
# Note: the PetShop API authenticates with JWT, not client certs, and exposes no endpoint that
# echoes the presented certificate. So this proves the handshake SUCCEEDS with the cert in play
# (no TLS/cert-rejection failure), not that the server specifically required or inspected it.
# Point BASE at an mTLS-terminating endpoint (gateway / reverse proxy) for a stricter check.
source "$(dirname "${BASH_SOURCE[0]}")/../lib/common.sh"

echo "[mtls-handshake]"

# Only meaningful with a client cert over HTTPS — otherwise skip (not fail).
if [ -z "$CLIENT_CERT_PEM" ]; then
  echo "  [SKIP] no CLIENT_CERT_PEM set in env.sh — mTLS not configured"; exit 0
fi
case "$BASE" in
  https:*) ;;
  *) echo "  [SKIP] BASE is not https ($BASE) — mTLS needs TLS"; exit 0 ;;
esac

# Verbose request: curl writes the TLS handshake trace to stderr, the body to a file,
# the HTTP status to stdout. COMMON_ARGS already carries --cert/--key/--cacert.
verbose="$OUT/mtls.verbose.txt"
body="$OUT/mtls.json"
set +e
code=$(curl -sv -o "$body" -w '%{http_code}' \
       "${COMMON_ARGS[@]+"${COMMON_ARGS[@]}"}" "${AUTH[@]}" \
       "$BASE/api/pets?page=1&pageSize=1" 2> "$verbose")
ec=$?
set -e
trace="$(cat "$verbose")"

# 1. No transport/TLS error (35 = handshake, 58 = client cert, 60 = server verify, ...).
if [ "$ec" -eq 0 ]; then pass "curl exit 0 (no TLS/transport failure)"
else fail "curl exit $ec — TLS/transport failure (responses/mtls.verbose.txt)"; fi

# 2. A TLS session was actually negotiated.
if echo "$trace" | grep -Eq 'SSL connection using|TLS[a-z0-9.]* handshake|TLSv1'; then
  pass "TLS session established"
else fail "no TLS handshake in verbose trace (responses/mtls.verbose.txt)"; fi

# 3. The certificate was not rejected by either side.
if echo "$trace" | grep -Eq 'SSL certificate problem|alert (bad|unknown|certificate)|handshake failure'; then
  fail "certificate rejected during handshake (responses/mtls.verbose.txt)"
else pass "certificate accepted (no rejection in handshake)"; fi

# 4. The server returned an HTTP response over the mTLS connection.
if echo "$code" | grep -Eq '^[0-9]{3}$'; then pass "HTTP response received over mTLS ($code)"
else fail "no HTTP response — handshake likely failed"; fi

finish
