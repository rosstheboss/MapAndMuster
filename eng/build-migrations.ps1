$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Push-Location $root
try {
    dotnet tool restore
    dotnet ef migrations bundle `
        --project (Join-Path $root 'src/Campaign.Infrastructure/Campaign.Infrastructure.csproj') `
        --startup-project (Join-Path $root 'src/Campaign.Api/Campaign.Api.csproj') `
        --configuration Release `
        --output (Join-Path $artifacts 'efbundle') `
        --force
}
finally {
    Pop-Location
}
