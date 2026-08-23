$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$bundle = Join-Path $artifacts 'efbundle.exe'

Push-Location $root
try {
    dotnet tool restore
    dotnet restore MapAndMuster.sln
    dotnet ef migrations bundle `
        --project (Join-Path $root 'src/MapAndMuster.Infrastructure/MapAndMuster.Infrastructure.csproj') `
        --startup-project (Join-Path $root 'src/MapAndMuster.Api/MapAndMuster.Api.csproj') `
        --configuration Release `
        --output $bundle `
        --force
}
finally {
    Pop-Location
}
