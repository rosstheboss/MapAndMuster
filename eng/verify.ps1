$ErrorActionPreference = 'Stop'

dotnet restore MapAndMuster.sln
dotnet format MapAndMuster.sln --verify-no-changes --no-restore
dotnet build MapAndMuster.sln --configuration Release --no-restore
dotnet test MapAndMuster.sln --configuration Release --no-build
npm --prefix src/MapAndMuster.Web ci
npm --prefix src/MapAndMuster.Web run verify
npm --prefix tests/MapAndMuster.Web.E2E ci
npm --prefix tests/MapAndMuster.Web.E2E test

