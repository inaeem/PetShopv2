# Central config for the curl smoke tests.
#
#   Copy this file to env.ps1 (gitignored) and edit.
#   lib\common.ps1 dot-sources env.ps1 automatically if it exists.
#   Environment variables ($env:BASE, $env:PETSHOP_TOKEN) override what's set here.

# Base URL of the running API.
$Base = 'http://localhost:5185'

# Bearer token. Set it here, OR leave blank and use $env:PETSHOP_TOKEN / token.txt.
$Token = ''

# An EXISTING category id in the target database (there is no seed step and no
# category-creation endpoint). The create/update bodies use this id.
$CategoryId = 1

# Extra headers sent on EVERY request (header name -> value).
$ExtraHeaders = @{
    # 'X-Api-Version'    = '1'
    # 'X-Correlation-Id' = 'curl-smoke'
}

# --- Mutual-TLS / certificates (all optional) ---

# Client certificate for mTLS (PEM). Point at the .pem holding the certificate.
# If the private key is in a SEPARATE file, also set $ClientKeyPem; if the key is
# inside the same PEM, leave $ClientKeyPem blank.
$ClientCertPem = ''     # e.g. 'C:\certs\client.pem'
$ClientKeyPem  = ''     # e.g. 'C:\certs\client.key'

# CA bundle (PEM) used to verify the server certificate. Blank = use the system store.
$CaCertPem = ''         # e.g. 'C:\certs\ca.pem'

# Skip TLS verification entirely (self-signed dev servers only). Avoid in CI.
$Insecure = $false
