# Runs every use-case script under cases\ in its own process and reports an aggregate.
# Each case runs separately, so one failure doesn't stop the rest.
#
#   $env:BASE = 'http://localhost:5185'
#   $env:PETSHOP_TOKEN = 'eyJ...'
#   pwsh -ExecutionPolicy Bypass -File tests\curl\run.ps1
#   pwsh -File tests\curl\run.ps1 cases\create-pet.ps1   # selected case(s)
#
# (Use 'powershell' instead of 'pwsh' on Windows PowerShell 5.1.)

$ErrorActionPreference = 'Continue'
$Root = $PSScriptRoot
$exe  = (Get-Process -Id $PID).Path   # current powershell/pwsh executable

if ($args.Count -gt 0) {
  $cases = $args | ForEach-Object { (Resolve-Path $_).Path }
} else {
  $cases = Get-ChildItem (Join-Path $Root 'cases') -Filter *.ps1 |
           Sort-Object Name | Select-Object -ExpandProperty FullName
}

$pass = 0; $fail = 0
foreach ($c in $cases) {
  Write-Host "-- $(Split-Path -Leaf $c) --"
  & $exe -NoProfile -ExecutionPolicy Bypass -File $c
  if ($LASTEXITCODE -eq 0) { $pass++ } else { $fail++ }
  Write-Host ''
}

Write-Host '============================'
Write-Host "use cases passed: $pass, failed: $fail"
if ($fail -ne 0) { exit 1 }
