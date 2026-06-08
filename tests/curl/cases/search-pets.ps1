# Use case: search via the dbo.usp_SearchPets stored procedure finds a pet by term.
. (Join-Path $PSScriptRoot '..\lib\common.ps1')

Write-Host '[search-pets]'
$null = New-Pet
Invoke-Api search 200 @Auth "$Base/api/pets/search?term=Rex"
Expect-Json search { param($r) $r.success -and (@($r.data.name) -contains 'Rex') } 'Rex found'

Complete-Case
