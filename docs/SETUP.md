# Solution Setup

## Prerequisites

- .NET 10 SDK matching `global.json`.
- Node 24.19.0, pinned in `.nvmrc` and `src/MapAndMuster.Web/.nvmrc`.
- Docker-compatible container runtime for PostgreSQL integration tests and local services.
- Visual Studio Community/Professional with web tooling, Visual Studio Code, or Cursor.

## Solution layout

`MapAndMuster.sln` contains:

```text
src/MapAndMuster.Domain
src/MapAndMuster.Application
src/MapAndMuster.Infrastructure
src/MapAndMuster.Api
tests/MapAndMuster.Backend.UnitTests
tests/MapAndMuster.Api.IntegrationTests
```

Angular lives in `src/MapAndMuster.Web` and is built with the Angular CLI and npm. Playwright end-to-end tests live in `tests/MapAndMuster.Web.E2E`. Do not add the Visual Studio JavaScript project to `MapAndMuster.sln`; CI builds the .NET solution only.

## Project references

```text
MapAndMuster.Application -> MapAndMuster.Domain
MapAndMuster.Infrastructure -> MapAndMuster.Application, MapAndMuster.Domain
MapAndMuster.Api -> MapAndMuster.Application, MapAndMuster.Infrastructure
MapAndMuster.Backend.UnitTests -> MapAndMuster.Domain, MapAndMuster.Application
MapAndMuster.Api.IntegrationTests -> MapAndMuster.Api
```

Backend package versions are centrally managed in `Directory.Packages.props`.

## Frontend quality tools

`src/MapAndMuster.Web` uses Angular ESLint, Prettier, Stylelint, and Vitest. Scripts from `config/package-scripts.json` are merged into `src/MapAndMuster.Web/package.json`. ESLint and Stylelint configuration is copied from `config/` into the Angular root. Package versions are pinned in `package-lock.json`.

## Local services

Start PostgreSQL and the development email catcher with:

```bash
docker compose up -d
```

Copy `.env.example` to a gitignored `.env` file, or store the same values in ASP.NET user secrets for `MapAndMuster.Api`. Do not commit production credentials. Environment and secret conventions are in `docs/environments.md` and `docs/secrets.md`.

Restore the local `dotnet-ef` tool with `dotnet tool restore` from the repository root when adding or inspecting EF Core migrations. The API startup project references `Microsoft.EntityFrameworkCore.Design` (PrivateAssets) so `dotnet ef` and `eng/build-migrations.*` can run. Production schema deployment uses `eng/build-migrations.*` and `eng/run-migrations.*`; see `docs/deployment.md`.

The Angular dev server proxies `/api` to `http://localhost:5219`. Start PostgreSQL and Mailpit, then run `MapAndMuster.Api` with the **http** launch profile (`http://localhost:5219`) so migrations apply. In Development the API does not redirect HTTP to HTTPS; that redirect would send the browser to `https://localhost:7247` and fail CORS from `http://localhost:4200`. Then run `npm --prefix src/MapAndMuster.Web start`. Confirmation and password-reset emails appear in Mailpit at `http://localhost:8025`. The API health endpoints are `GET /health/live`, `GET /health/ready`, and `GET /health`.

To send those messages to a real inbox while testing, override SMTP in user secrets (do not commit credentials). Restart `MapAndMuster.Api` after changing them. Sign up with the address that should receive the mail, or call resend-confirmation for an existing unconfirmed account. Example Gmail app-password settings:

```bash
dotnet user-secrets set "Email:SmtpHost" "smtp.gmail.com" --project src/MapAndMuster.Api
dotnet user-secrets set "Email:SmtpPort" "587" --project src/MapAndMuster.Api
dotnet user-secrets set "Email:EnableSsl" "true" --project src/MapAndMuster.Api
dotnet user-secrets set "Email:FromAddress" "you@gmail.com" --project src/MapAndMuster.Api
dotnet user-secrets set "Email:SmtpUsername" "you@gmail.com" --project src/MapAndMuster.Api
dotnet user-secrets set "Email:SmtpPassword" "your-app-password" --project src/MapAndMuster.Api
```

Leave `Email:SmtpUsername` empty to keep using Mailpit. Production uses Resend (`Email:Provider=Resend` and `Email:Resend:ApiKey`); see `docs/adr/0003-production-hosting-stack.md`.

## Seeded identity

Do not put Identity users or password hashes in EF migrations. `IdentityMaintenance` runs on every API start after optional `MigrateAsync`, including Production when `Database:ApplyMigrationsOnStartup` is false.

- Privileged administrator: username `rosstheboss`, email `ross.gustafson@gmail.com`. If that account is missing, the API creates it from `Identity:BootstrapAdminPassword` (`Identity__BootstrapAdminPassword`) and assigns the Administrator role. An existing account is promoted if needed and its password is never overwritten. Production and Staging require the bootstrap password so a blank database can create the account; after the first login, change the password in-app. Development may leave the value empty when the account already exists.
- Test 1–Test 30 (`test1`…`test30`) are created outside the Testing environment. They cannot password-login or use public site chat. Sign in as the administrator and use **Test users** to impersonate them.

Set a bootstrap password for a blank local database:

```bash
dotnet user-secrets set "Identity:BootstrapAdminPassword" "your-local-bootstrap-password" --project src/MapAndMuster.Api
```

Google, Facebook, and Discord sign-in are optional. Leave those settings empty for email-only development. To enable a provider, set the client id/secret (Facebook uses AppId/AppSecret) in user secrets and register these callback URLs with the provider. Production callback registration is in `docs/authentication-production.md`.

- `http://localhost:4200/api/auth/external/google/callback`
- `http://localhost:4200/api/auth/external/facebook/callback`
- `http://localhost:4200/api/auth/external/discord/callback`

## Verification

Run `eng/verify.ps1` or `eng/verify.sh` from the repository root. GitHub Actions runs the same
logical checks plus API Docker image builds and EF migration bundles against a temporary PostgreSQL
service (`.github/workflows/ci.yml`). A scheduled nightly workflow (`.github/workflows/nightly.yml`)
repeats CI and adds NuGet/npm audits. Neither workflow deploys production. After a host is live,
run `scripts/smoke-test.ps1` or GitHub **Actions → Smoke test**. Operator steps are in
`docs/human-deployment-checklist.md`.
