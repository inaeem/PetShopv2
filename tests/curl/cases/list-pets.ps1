# Use case: a freshly-created pet appears in the owner's paged list.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[list-pets]'
$null = New-Pet
Invoke-Api list-pets 200 @Auth "$Base/api/pets?page=1&pageSize=50"
Expect-Json list-pets { param($r) $r.success -and (@($r.data.items.name) -contains 'Rex') } 'Rex in list'

Complete-Case
