#!/usr/bin/env bash
#
# Shared config + helpers for the bash curl use-case scripts.
# Sourced by every cases/*.sh and by run.sh — not run on its own.
#
# Works in any bash (macOS, Linux, and the VS Code / Git Bash terminal on Windows,
# which bundles curl). Needs jq for response validation.
set -euo pipefail

# Harness root from THIS file's location, so cases work standalone or via run.sh.
LIB_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$LIB_DIR/.." && pwd)"
FIX="$ROOT/fixtures"
OUT="$ROOT/responses"
mkdir -p "$OUT"

command -v curl >/dev/null || { echo "curl is required."; exit 2; }
command -v jq   >/dev/null || { echo "jq is required (brew install jq / choco install jq / apt install jq)."; exit 2; }

# Capture any shell-environment values first; they win over env.sh below.
_env_base="${BASE:-}"
_env_token="${PETSHOP_TOKEN:-}"

# --- defaults (overridden by env.sh, then by environment variables) ---
BASE="http://localhost:5185"   # API base URL
TOKEN=""                        # bearer token
CATEGORY_ID=1                   # existing category id used by create/update bodies
EXTRA_HEADERS=()                # extra headers sent on every request ("Name: Value")
CLIENT_CERT_PEM=""              # mutual-TLS client cert (PEM) -> curl --cert
CLIENT_KEY_PEM=""               # private key (PEM) if separate -> curl --key
CA_CERT_PEM=""                  # CA bundle to verify the server (PEM) -> curl --cacert
INSECURE=0                      # 1 = curl -k (skip TLS verification, dev self-signed)

# Central config file (gitignored). Copy env.example.sh -> env.sh and edit.
[ -f "$ROOT/env.sh" ] && source "$ROOT/env.sh"

# Environment variables win over the file (handy for CI / one-off overrides).
[ -n "$_env_base" ]  && BASE="$_env_base"
[ -n "$_env_token" ] && TOKEN="$_env_token"

# Token fallback: not set above -> try token.txt.
if [ -z "$TOKEN" ] && [ -f "$ROOT/token.txt" ]; then
  TOKEN="$(tr -d '[:space:]' < "$ROOT/token.txt")"
fi
[ -z "$TOKEN" ] && { echo "No token. Set it in env.sh, \$PETSHOP_TOKEN, or tests/curl/token.txt"; exit 2; }

# --- curl args sent on EVERY request: extra headers + TLS/cert options ---
COMMON_ARGS=()
if [ "${#EXTRA_HEADERS[@]}" -gt 0 ]; then
  for h in "${EXTRA_HEADERS[@]}"; do COMMON_ARGS+=(-H "$h"); done
fi
if [ -n "$CLIENT_CERT_PEM" ]; then
  [ -f "$CLIENT_CERT_PEM" ] || { echo "CLIENT_CERT_PEM not found: $CLIENT_CERT_PEM"; exit 2; }
  COMMON_ARGS+=(--cert "$CLIENT_CERT_PEM" --cert-type PEM)
  if [ -n "$CLIENT_KEY_PEM" ]; then
    [ -f "$CLIENT_KEY_PEM" ] || { echo "CLIENT_KEY_PEM not found: $CLIENT_KEY_PEM"; exit 2; }
    COMMON_ARGS+=(--key "$CLIENT_KEY_PEM" --key-type PEM)
  fi
fi
if [ -n "$CA_CERT_PEM" ]; then
  [ -f "$CA_CERT_PEM" ] || { echo "CA_CERT_PEM not found: $CA_CERT_PEM"; exit 2; }
  COMMON_ARGS+=(--cacert "$CA_CERT_PEM")
fi
[ "$INSECURE" = "1" ] && COMMON_ARGS+=(-k)

AUTH=(-H "Authorization: Bearer $TOKEN")
JSON=(-H "Content-Type: application/json")

FAILS=0
pass() { echo "  [PASS] $1"; }
fail() { echo "  [FAIL] $1"; FAILS=$((FAILS + 1)); }

# call <name> <expected-status> <curl-args...>
# Prepends the shared headers/cert options, captures the body to responses/<name>.json,
# and asserts the HTTP status.
call() {
  local name="$1" want="$2"; shift 2
  local got
  got=$(curl -s -o "$OUT/$name.json" -w '%{http_code}' \
        "${COMMON_ARGS[@]+"${COMMON_ARGS[@]}"}" "$@")
  if [ "$got" = "$want" ]; then pass "$name -> $got"
  else fail "$name expected $want, got $got (responses/$name.json)"; fi
}

# expect_json <name> <jq-filter>
# Asserts the captured body satisfies a jq boolean filter. 'jq -e' also fails on
# malformed JSON, catching "returned 200 but the body wasn't the envelope".
expect_json() {
  local name="$1" filter="$2"
  if jq -e "$filter" "$OUT/$name.json" >/dev/null 2>&1; then pass "$name body: $filter"
  else fail "$name body failed: $filter (responses/$name.json)"; fi
}

# new_request_body <fixture-name>   (echoes the path to a generated body)
# Loads fixtures/<name>.json, injects the configured CATEGORY_ID, and writes the
# body to responses/_req-<name>.json — so the harness doesn't depend on a seeded id.
new_request_body() {
  local fixture="$1" path="$OUT/_req-$1.json"
  jq ".categoryId = $CATEGORY_ID" "$FIX/$fixture.json" > "$path"
  echo "$path"
}

# Creates a pet (using CATEGORY_ID) and echoes its new id — for cases that need a
# pre-existing pet (get / update / delete / search) so they stay self-contained.
create_pet() {
  local body; body="$(new_request_body create-pet)"
  curl -s -o "$OUT/_seed-pet.json" \
    "${COMMON_ARGS[@]+"${COMMON_ARGS[@]}"}" "${AUTH[@]}" "${JSON[@]}" \
    -X POST "$BASE/api/pets" -d @"$body"
  jq -r '.data.id' "$OUT/_seed-pet.json"
}

# Each case calls this last; exits non-zero if any check failed so run.sh can tally.
finish() {
  if [ "$FAILS" -ne 0 ]; then echo "  -> $FAILS check(s) failed"; exit 1; fi
  exit 0
}
