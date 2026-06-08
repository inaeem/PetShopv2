# Use case: update an existing pet's fields (price changes 650 -> 700).
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[update-pet]'
$id = New-Pet
$body = New-RequestBody 'update-pet' @{ categoryId = $CategoryId }
Invoke-Api update-pet 200 @Auth @Json -X PUT "$Base/api/pets/$id" -d "@$body"
Expect-Json update-pet { param($r) $r.success -and ($r.data.price -eq 700) } 'price=700'

Complete-Case
