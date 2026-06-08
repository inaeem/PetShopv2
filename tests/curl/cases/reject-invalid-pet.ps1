# Use case: an invalid create payload is rejected with 400 and validation errors.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[reject-invalid-pet]'
Invoke-Api create-invalid 400 @Auth @Json -X POST "$Base/api/pets" -d "@$(Join-Path $Fix 'create-pet-invalid.json')"
Expect-Json create-invalid { param($r) (-not $r.success) -and (@($r.errors).Count -gt 0) } 'success=false + errors'

Complete-Case
