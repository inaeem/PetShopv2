#!/usr/bin/env bash
#
# One-shot release: build + test, then publish the deployable package.
# Equivalent to running scripts/build.sh followed by scripts/publish.sh.
#
# Usage:
#   scripts/release.sh                      # Release build, run tests, framework-dependent publish
#   RUN_TESTS=false scripts/release.sh      # skip the e2e tests
#   SELF_CONTAINED=true scripts/release.sh  # bundle the runtime (no .NET needed on the server)
#   RUNTIME=win-x64 scripts/release.sh      # target server RID (default win-x64)
#
# Env vars (CONFIG, RUN_TESTS, RUNTIME, SELF_CONTAINED, OUT) are passed through to
# the underlying scripts.
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "########## 1/2 build & test ##########"
"$SCRIPT_DIR/build.sh"

echo "########## 2/2 publish ##########"
"$SCRIPT_DIR/publish.sh"

echo "########## release complete ##########"
