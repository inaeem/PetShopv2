# Use case: fetch a single pet by id.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[get-pet]'
$id = New-Pet
Invoke-Api get-pet 200 @Auth "$Base/api/pets/$id"
Expect-Json get-pet { param($r) $r.success -and ($r.data.id -eq $id) -and ($r.data.name -eq 'Rex') } 'id + name match'

Complete-Case
