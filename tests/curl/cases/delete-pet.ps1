# Use case: an Admin deletes a pet, and it is gone (404) afterwards.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[delete-pet]'
$id = New-Pet
Invoke-Api delete-pet 200 @Auth -X DELETE "$Base/api/pets/$id"
Expect-Json delete-pet { param($r) $r.success } 'success'

# It should no longer be retrievable.
Invoke-Api get-deleted 404 @Auth "$Base/api/pets/$id"
Expect-Json get-deleted { param($r) -not $r.success } 'success=false'

Complete-Case
