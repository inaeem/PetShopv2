# Central config for the curl smoke tests.
#
#   Copy this file to env.sh (gitignored) and edit.
#   lib/common.sh sources env.sh automatically if it exists.
#   Shell env vars (BASE, PETSHOP_TOKEN) override what's set here.

# Base URL of the running API.
BASE="http://localhost:5185"

# Bearer token. Set it here, OR leave blank and use $PETSHOP_TOKEN / token.txt.
TOKEN=""

# An EXISTING category id in the target database (there is no seed step and no
# category-creation endpoint). The create/update bodies use this id.
CATEGORY_ID=1

# Extra headers sent on EVERY request (one entry per "Name: Value").
EXTRA_HEADERS=(
  # "X-Api-Version: 1"
  # "X-Correlation-Id: curl-smoke"
)

# --- Mutual-TLS / certificates (all optional) ---

# Client certificate for mTLS (PEM). Point at the .pem holding the certificate.
# If the private key is in a SEPARATE file, also set CLIENT_KEY_PEM; if the key is
# inside the same PEM, leave CLIENT_KEY_PEM blank.
CLIENT_CERT_PEM=""     # e.g. "/c/certs/client.pem"
CLIENT_KEY_PEM=""      # e.g. "/c/certs/client.key"

# CA bundle (PEM) used to verify the server certificate. Blank = use the system store.
CA_CERT_PEM=""         # e.g. "/c/certs/ca.pem"

# Skip TLS verification entirely (self-signed dev servers only). 1 = on. Avoid in CI.
INSECURE=0
