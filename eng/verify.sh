#!/usr/bin/env bash
set -euo pipefail

dotnet restore Campaign.sln
dotnet format Campaign.sln --verify-no-changes --no-restore
dotnet build Campaign.sln --configuration Release --no-restore
dotnet test Campaign.sln --configuration Release --no-build
npm --prefix src/Campaign.Web ci
npm --prefix src/Campaign.Web run verify
npm --prefix tests/Campaign.Web.E2E ci
npm --prefix tests/Campaign.Web.E2E test

