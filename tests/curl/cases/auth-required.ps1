# Use case: the API rejects requests without a valid token, accepts them with one.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[auth-required]'
Invoke-Api unauth-list 401 "$Base/api/pets?page=1&pageSize=5"
Expect-Json unauth-list { param($r) (-not $r.success) -and $r.message } 'success=false + message'

Invoke-Api auth-list 200 @Auth "$Base/api/pets?page=1&pageSize=5"
Expect-Json auth-list { param($r) $r.success -and ($r.data.items -is [Array]) } 'success + items array'

Complete-Case
