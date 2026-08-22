param(
    [string] $ConnectionString = $env:ConnectionStrings__Campaign
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'Set ConnectionStrings__Campaign or pass -ConnectionString. Refusing to migrate an unspecified database.'
}

$root = Split-Path -Parent $PSScriptRoot
$bundle = Join-Path $root 'artifacts/efbundle.exe'
if (-not (Test-Path $bundle)) {
    $bundle = Join-Path $root 'artifacts/efbundle'
}

if (-not (Test-Path $bundle)) {
    throw 'No migration bundle found. Run eng/build-migrations.ps1 first.'
}

& $bundle --connection $ConnectionString
if ($LASTEXITCODE -ne 0) {
    throw "Migration bundle failed with exit code $LASTEXITCODE."
}
