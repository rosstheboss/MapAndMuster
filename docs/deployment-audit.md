# Deployment Audit

This audit records the repository as it existed when Phase 1 of
`docs/AGENT_DEPLOYMENT_PLAN.md` was implemented, plus the deviations taken from that
plan's sample names.

## Actual project structure

`Campaign.sln` contains:

```text
src/Campaign.Domain
src/Campaign.Application
src/Campaign.Infrastructure
src/Campaign.Api
tests/Campaign.Backend.UnitTests
tests/Campaign.Api.IntegrationTests
```

Angular lives in `src/Campaign.Web`. Playwright tests live in `tests/Campaign.Web.E2E`.
They are not Visual Studio solution projects.

## Executable services

| Component | Role | Production deployment |
|---|---|---|
| `Campaign.Api` | ASP.NET Core HTTP host, Identity, OpenAPI, health checks, EF migrations, hosted email outbox processor | Docker web service |
| `Campaign.Web` | Angular 22 static UI | Static site |
| PostgreSQL 17 | Authoritative store | Managed PostgreSQL |
| Mailpit | Local email catcher | Not deployed |
| Domain / Application / Infrastructure | Libraries compiled into the API | Not deployed |
| Tests | xUnit, Vitest, Playwright | CI only |

There is **no** `Campaign.Worker` project. ADR 0001 deploys only the API and the Angular app.
Background work is an `OutboxEmailProcessor` hosted service inside `Campaign.Api`. Deadline
transitions are command-driven and safe to repeat; a separate worker is not required for the
initial production topology.

## Configuration found

- Connection string: `ConnectionStrings:Campaign` / `ConnectionStrings__Campaign`
- EF Core: Npgsql, migrations in `src/Campaign.Infrastructure/Persistence/Migrations`
- Authentication: ASP.NET Core Identity plus optional Google, Facebook, and Discord
- Email: transactional outbox plus SMTP (local Mailpit) and Resend
- Logging: built-in ASP.NET Core logging; JSON console formatter in Production/Staging
- Angular API access: same-origin `/api` in development via `proxy.conf.json` (`http://localhost:5219`)
- Storage: local filesystem under `Storage:RootPath` (default `app-data`)

## Docker

- `docker-compose.yml` starts local PostgreSQL 17 and Mailpit only.
- Production API image: `src/Campaign.Api/Dockerfile`
- No Worker Dockerfile, by architecture.

## Tests

- `tests/Campaign.Backend.UnitTests` — domain and application unit tests
- `tests/Campaign.Api.IntegrationTests` — `WebApplicationFactory` plus Testcontainers PostgreSQL
- `src/Campaign.Web` Vitest
- `tests/Campaign.Web.E2E` Playwright
- CI: `.github/workflows/quality.yml` (Phase 2 will add dedicated `ci.yml` / `nightly.yml`)

## Secrets and development values in source

Tracked, non-production values:

- `docker-compose.yml` and `.env.example` use local database password `campaign`
- Design-time EF factory uses the same local connection string
- `IdentityMaintenance` promotes a hardcoded operator email/username (not a password or API key)

No production connection strings, OAuth secrets, or email API keys are committed.
`.env` and `.env.*` are gitignored except `.env.example`.

## Hard-coded development URLs and ports

| Port / URL | Use |
|---|---|
| `http://localhost:5219` | API HTTP launch profile and Angular proxy |
| `https://localhost:7247` | API HTTPS launch profile (not used with the Angular proxy) |
| `http://localhost:4200` | Angular dev server / `PublicWeb:Origin` default |
| `5432` | Local PostgreSQL |
| `1025` / `8025` | Mailpit SMTP / UI |
| `8080` | API container listen port (`PORT` overrides at runtime) |

## Migration strategy

EF Core migrations are committed. A clean database can be migrated from zero.
`Database:ApplyMigrationsOnStartup` defaults to `true` outside Production/Staging.
Production and Staging default it to `false`; use `eng/build-migrations.*` and
`eng/run-migrations.*` (or set the flag explicitly) so schema changes are deliberate.

## Deployment risks

- Cookie authentication assumes a same-origin `/api` reverse proxy. Splitting the Angular origin
  from the API origin requires CORS and cookie `SameSite` work that is not in this phase.
- File storage is local disk, not object storage. A replacement remains an open operations decision.
- The API container must not be exposed directly to the internet when forwarded headers are enabled.
- Automatic startup migrations are disabled in Production/Staging to avoid applying schema changes
  without an operator-approved pre-deploy step.
- External login is optional and silent when credentials are absent.

## Human actions eventually required

Account creation, billing, domain/DNS, Resend domain verification, OAuth console apps, production
PostgreSQL, secret entry, and deployment approval. See Phase 3 of the deployment plan.

## Deviations from the sample plan names

The plan's `OldWorldCampaign.*` names map to `Campaign.*`. There is no Worker executable; the API
image is the only backend container to build for Phase 1.
