# Use case: a user creates a pet and the response echoes it back with an id.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[create-pet]'
$body = New-RequestBody 'create-pet' @{ categoryId = $CategoryId }
Invoke-Api create-pet 201 @Auth @Json -X POST "$Base/api/pets" -d "@$body"
Expect-Json create-pet { param($r) $r.success -and $r.data.name -eq 'Rex' -and $r.data.id -gt 0 } 'success + Rex + id'

Complete-Case
