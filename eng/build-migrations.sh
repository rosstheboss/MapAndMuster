#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "${root}/artifacts"

cd "${root}"
dotnet tool restore
dotnet ef migrations bundle \
  --project "${root}/src/Campaign.Infrastructure/Campaign.Infrastructure.csproj" \
  --startup-project "${root}/src/Campaign.Api/Campaign.Api.csproj" \
  --configuration Release \
  --output "${root}/artifacts/efbundle" \
  --force
