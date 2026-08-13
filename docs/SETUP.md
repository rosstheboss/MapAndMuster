# Solution Setup

## Prerequisites

- .NET 10 SDK matching `global.json`.
- Node 24.19.0, pinned in `.nvmrc` and `src/Campaign.Web/.nvmrc`.
- Docker-compatible container runtime for PostgreSQL integration tests and local services.
- Visual Studio Community/Professional with web tooling, Visual Studio Code, or Cursor.

## Solution layout

`Campaign.sln` contains:

```text
src/Campaign.Domain
src/Campaign.Application
src/Campaign.Infrastructure
src/Campaign.Api
tests/Campaign.Backend.UnitTests
tests/Campaign.Api.IntegrationTests
```

Angular lives in `src/Campaign.Web` and is built with the Angular CLI and npm. Playwright end-to-end tests live in `tests/Campaign.Web.E2E`. Do not add the Visual Studio JavaScript project to `Campaign.sln`; CI builds the .NET solution only.

## Project references

```text
Campaign.Application -> Campaign.Domain
Campaign.Infrastructure -> Campaign.Application, Campaign.Domain
Campaign.Api -> Campaign.Application, Campaign.Infrastructure
Campaign.Backend.UnitTests -> Campaign.Domain, Campaign.Application
Campaign.Api.IntegrationTests -> Campaign.Api
```

Backend package versions are centrally managed in `Directory.Packages.props`.

## Frontend quality tools

`src/Campaign.Web` uses Angular ESLint, Prettier, Stylelint, and Vitest. Scripts from `config/package-scripts.json` are merged into `src/Campaign.Web/package.json`. ESLint and Stylelint configuration is copied from `config/` into the Angular root. Package versions are pinned in `package-lock.json`.

## Local services

Start PostgreSQL and the development email catcher with:

```bash
docker compose up -d
```

Copy `.env.example` to a gitignored `.env` file, or store the same values in ASP.NET user secrets for `Campaign.Api`. Do not commit production credentials.

Restore the local `dotnet-ef` tool with `dotnet tool restore` from the repository root when adding or inspecting EF Core migrations.

The Angular dev server proxies `/api` to `http://localhost:5219`. Start PostgreSQL and Mailpit, apply migrations by running `Campaign.Api`, then run `npm --prefix src/Campaign.Web start`. Confirmation and password-reset emails appear in Mailpit at `http://localhost:8025`.

Google, Facebook, and Discord sign-in are optional. Leave those settings empty for email-only development. To enable a provider, set the client id/secret (Facebook uses AppId/AppSecret) in user secrets and register these callback URLs with the provider:

- `http://localhost:4200/api/auth/external/google/callback`
- `http://localhost:4200/api/auth/external/facebook/callback`
- `http://localhost:4200/api/auth/external/discord/callback`

## Verification

Run `eng/verify.ps1` or `eng/verify.sh` from the repository root. CI runs the same logical checks.
