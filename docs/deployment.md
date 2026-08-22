# Deployment (Phase 1–2)

This is the repository runbook for production-ready artifacts and CI. Account provisioning, DNS, and the
Render blueprint belong to later phases of `docs/AGENT_DEPLOYMENT_PLAN.md`.

## Artifacts

- API Docker image from `src/Campaign.Api/Dockerfile`
- Angular production build from `npm --prefix src/Campaign.Web run build` (also part of `npm run verify`)
- EF Core migration bundle from `eng/build-migrations.ps1` or `eng/build-migrations.sh`

There is no Worker image. Email outbox processing runs inside the API process.

## API container

Build from the repository root:

```bash
docker build -f src/Campaign.Api/Dockerfile -t campaign-api .
```

The image listens on `8080` as a non-root user. Render's `PORT` value overrides that at runtime.
Bind-mount or set `Storage__RootPath` if uploaded files must survive container replacement.

Local run against Docker Compose PostgreSQL:

```bash
docker run --rm -p 8080:8080 --env-file .env -e ConnectionStrings__Campaign="Host=host.docker.internal;Port=5432;Database=campaign;Username=campaign;Password=campaign" campaign-api
```

Use `ASPNETCORE_ENVIRONMENT=Development` or supply Production/Staging variables listed in
`docs/environments.md`. Production defaults do not apply migrations on startup.

## Health checks

| Path | Meaning |
|---|---|
| `GET /health/live` | Process is running |
| `GET /health/ready` | PostgreSQL is reachable when a connection string is configured |
| `GET /health` | Same checks as ready (use this on the load balancer) |

Responses are `{"status":"Healthy"}` or an equivalent status string. They omit connection strings,
exceptions, and check details. Unhealthy ready checks use HTTP 503.

## Logging

- Development: default console formatter
- Production/Staging: JSON console formatter with scopes and UTC timestamps
- Every response includes `X-Correlation-ID` (echoed when the incoming value is a short safe token)
- Log scopes include `CorrelationId` and `RequestId`
- Do not log secrets; see `docs/secrets.md`

User, campaign, turn, and battle identifiers appear in application messages when those operations
already log them. They are not attached globally.

## Angular static site

`ng build` emits hashed bundles under `src/Campaign.Web/dist/campaign-web/browser` (Angular 22
application builder). The host must:

1. Serve `index.html` for unknown frontend routes (SPA fallback).
2. Route `/api` and `/health` to the API, not to `index.html`.
3. Optionally replace `config.json` `apiBaseUrl` at deploy time. Leave it empty for same-origin `/api`.

Do not put secrets in `config.json`.

## Database migrations

Committed migrations live in `src/Campaign.Infrastructure/Persistence/Migrations`.

Development applies them on API startup when `ConnectionStrings:Campaign` is set.

Production/Staging default `Database:ApplyMigrationsOnStartup` to `false`. Build a bundle on a
machine that has the .NET SDK, then run it against an **explicit** connection string:

```bash
./eng/build-migrations.sh
ConnectionStrings__Campaign="<RENDER_DATABASE_URL>" ./eng/run-migrations.sh
```

Windows:

```powershell
./eng/build-migrations.ps1
./eng/run-migrations.ps1 -ConnectionString "<RENDER_DATABASE_URL>"
```

The run scripts refuse to start when no connection string is provided. Do not point them at
production until that database has been approved and backed up.

### Expand/contract

Prefer additive schema changes (new nullable columns, new tables) that the running API can ignore,
then deploy code that uses them, then remove unused columns in a later release. Avoid one-step
renames or destructive drops against a live campaign database.

## Continuous integration

`.github/workflows/ci.yml` runs on pull requests, pushes to `main`, and `workflow_dispatch`. It
does not deploy.

Jobs:

- backend restore, format, Release build, and tests (including Testcontainers PostgreSQL)
- Angular `npm ci` and `npm run verify` (lint, unit tests, production build)
- API Docker image build (no push; no Worker image)
- EF Core migration bundle applied to an empty GitHub Actions PostgreSQL 17 service
- Playwright after backend and frontend succeed

`.github/workflows/nightly.yml` runs around 02:00 America/Indiana/Indianapolis (06:00 UTC during
EDT) and on `workflow_dispatch`. It reuses `ci.yml` and adds NuGet high/critical audit plus
`npm audit --audit-level=high`. It does not deploy.

## Local development

`docker compose up -d` still starts PostgreSQL and Mailpit only. Run the API and Angular as
documented in `docs/SETUP.md`.
