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

Copy `.env.example` to a gitignored `.env` file, or store the same values in ASP.NET user secrets for `Campaign.Api`. Do not commit production credentials. Environment and secret conventions are in `docs/environments.md` and `docs/secrets.md`.

Restore the local `dotnet-ef` tool with `dotnet tool restore` from the repository root when adding or inspecting EF Core migrations. The API startup project references `Microsoft.EntityFrameworkCore.Design` (PrivateAssets) so `dotnet ef` and `eng/build-migrations.*` can run. Production schema deployment uses `eng/build-migrations.*` and `eng/run-migrations.*`; see `docs/deployment.md`.

The Angular dev server proxies `/api` to `http://localhost:5219`. Start PostgreSQL and Mailpit, then run `Campaign.Api` with the **http** launch profile (`http://localhost:5219`) so migrations apply. In Development the API does not redirect HTTP to HTTPS; that redirect would send the browser to `https://localhost:7247` and fail CORS from `http://localhost:4200`. Then run `npm --prefix src/Campaign.Web start`. Confirmation and password-reset emails appear in Mailpit at `http://localhost:8025`. The API health endpoints are `GET /health/live`, `GET /health/ready`, and `GET /health`.

To send those messages to a real inbox while testing, override SMTP in user secrets (do not commit credentials). Restart `Campaign.Api` after changing them. Sign up with the address that should receive the mail, or call resend-confirmation for an existing unconfirmed account. Example Gmail app-password settings:

```bash
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com" --project src/Campaign.Api
dotnet user-secrets set "Email:SmtpPort" "587" --project src/Campaign.Api
dotnet user-secrets set "Email:EnableSsl" "true" --project src/Campaign.Api
dotnet user-secrets set "Email:FromAddress" "you@gmail.com" --project src/Campaign.Api
dotnet user-secrets set "Email:SmtpUsername" "you@gmail.com" --project src/Campaign.Api
dotnet user-secrets set "Email:SmtpPassword" "your-app-password" --project src/Campaign.Api
```

Leave `Email:SmtpUsername` empty to keep using Mailpit. Production uses Resend (`Email:Provider=Resend` and `Email:Resend:ApiKey`); see `docs/adr/0003-production-hosting-stack.md`.

Google, Facebook, and Discord sign-in are optional. Leave those settings empty for email-only development. To enable a provider, set the client id/secret (Facebook uses AppId/AppSecret) in user secrets and register these callback URLs with the provider. Production callback registration is in `docs/authentication-production.md`.

- `http://localhost:4200/api/auth/external/google/callback`
- `http://localhost:4200/api/auth/external/facebook/callback`
- `http://localhost:4200/api/auth/external/discord/callback`

## Verification

Run `eng/verify.ps1` or `eng/verify.sh` from the repository root. GitHub Actions runs the same
logical checks plus API Docker image builds and EF migration bundles against a temporary PostgreSQL
service (`.github/workflows/ci.yml`). A scheduled nightly workflow (`.github/workflows/nightly.yml`)
repeats CI and adds NuGet/npm audits. Neither workflow deploys production.
