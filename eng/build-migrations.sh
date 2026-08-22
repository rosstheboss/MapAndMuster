#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "${root}/artifacts"

cd "${root}"
dotnet tool restore
dotnet restore MapAndMuster.sln
dotnet ef migrations bundle \
  --project "${root}/src/MapAndMuster.Infrastructure/MapAndMuster.Infrastructure.csproj" \
  --startup-project "${root}/src/MapAndMuster.Api/MapAndMuster.Api.csproj" \
  --configuration Release \
  --output "${root}/artifacts/efbundle" \
  --force
