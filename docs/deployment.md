# Deployment

This is the human-facing runbook for GitHub, Render, Cloudflare, Resend, and optional OAuth.
Account creation, billing, DNS, and secret entry are operator-owned. The repository already
builds artifacts and CI; this file explains how to turn those into a running site.

Do **not** apply `render.yaml` until you have approved billing for Render, Cloudflare, Resend,
and the domain. Applying the Blueprint creates billable resources.

There is no Worker service. Email outbox processing runs inside `mapandmuster-api`. See
`docs/adr/0003-production-hosting-stack.md`.

Operator checkboxes live in `docs/human-deployment-checklist.md`. Staging is in
`docs/staging.md`. Backups are in `docs/database-backup-restore.md`.

## Placeholders

Replace these when you provision. Never commit the real values.

```text
Public origin               https://mapandmuster.com (no www)
<API_RENDER_HOST>          mapandmuster-api.onrender.com (or the custom api host)
<WEB_RENDER_HOST>          mapandmuster-web.onrender.com
<RENDER_DATABASE_URL>      External postgres:// URI from the Render database
<RESEND_API_KEY>           Resend API key
<IDENTITY_BOOTSTRAP_ADMIN_PASSWORD>  Password used only if rosstheboss does not exist yet
<EMAIL_FROM_ADDRESS>       noreply@mapandmuster.com (must be verified in Resend)
<GOOGLE_CLIENT_ID>
<GOOGLE_CLIENT_SECRET>
<DISCORD_CLIENT_ID>
<DISCORD_CLIENT_SECRET>
<FACEBOOK_APP_ID>
<FACEBOOK_APP_SECRET>
```

## Prerequisites

- GitHub repository with Actions enabled and a green CI run on `master`
  (`.github/workflows/ci.yml`)
- Render account and an approved paid plan (Postgres is not free)
- Cloudflare account and a domain you control
- Resend account
- Optional: Google Cloud, Discord Developer, and Meta developer apps
- Docker on your workstation for migration bundles, dumps, and restores
- .NET 10 SDK matching `global.json` to build EF bundles

## Repository readiness

A fresh checkout can:

- restore, format, build, and test the .NET solution
- run Angular `npm run verify`
- build `src/MapAndMuster.Api/Dockerfile`
- apply EF migrations to an empty PostgreSQL 17 database

CI does that on every pull request and `master` push. Nightly re-runs CI and audits
dependencies. Neither workflow deploys. The smoke workflow
(`.github/workflows/smoke-test.yml`) is manual `workflow_dispatch` only.

## Artifacts

- API Docker image from `src/MapAndMuster.Api/Dockerfile`
- Angular production build from `npm --prefix src/MapAndMuster.Web run build`
- EF Core migration bundle from `eng/build-migrations.ps1` or `eng/build-migrations.sh`

Local run of the API container is documented at the end of this file.

## Render account setup

1. Create a Render workspace at [dashboard.render.com](https://dashboard.render.com).
2. Connect the GitHub repository.
3. Confirm billing and a Postgres plan you accept. The Blueprint does not pin a plan;
   Render defaults new databases to its cheapest paid size.
4. Region in `render.yaml` is `ohio` (closest to Indianapolis). It cannot change after
   create. Edit the file before the first apply if you want another region.

## Apply the Blueprint

`render.yaml` defines:

| Resource | Render name | Role |
|---|---|---|
| Postgres 17 | `mapandmuster-db` | Authoritative store |
| Docker web service | `mapandmuster-api` | API, Identity, health, email outbox, uploads disk |
| Static site | `mapandmuster-web` | Angular `dist/mapandmuster-web/browser` |

There is no Worker. Do not add one.

1. In Render: **Blueprints → New Blueprint Instance**.
2. Select this repository. Path is `render.yaml` at the repo root.
3. Confirm plans and region.
4. Enter every `sync: false` value (see below). Do not paste secrets into Git.
5. Apply. Wait until Postgres is available before expecting `/health` to be ready.

Auto-deploy is `checksPass`: Render deploys `master` only after GitHub checks pass.

### Values to enter at apply time

| Render key | What to type | Where to get it |
|---|---|---|
| `PublicWeb__Origin` | `https://mapandmuster.com` | The public site origin after Cloudflare is live. Until then you may use `https://<WEB_RENDER_HOST>` for a first bring-up, then change it. Production rejects a hostname with a `staging` DNS label. |
| `Email__FromAddress` | `noreply@mapandmuster.com` | Address on a domain you will verify in Resend |
| `Email__Resend__ApiKey` | `<RESEND_API_KEY>` | Resend dashboard → API Keys |
| `Identity__BootstrapAdminPassword` | A unique password that meets the site policy (12+ characters, upper, lower, digit, special) | Password manager. Used only if `rosstheboss` does not exist yet |

`Email__FromName` defaults to `Map & Muster`.

`ConnectionStrings__Campaign` is bound from `mapandmuster-db` (internal `postgres://` URI).
The API converts that URI to Npgsql keyword form on startup. Do not paste the production URL
into staging.

Optional Google / Discord / Facebook keys are **not** in the Blueprint. Add them later
on `mapandmuster-api` in **Environment**. Empty values keep email-and-password sign-in. Keys
are listed in `docs/environments.md`.

## Render PostgreSQL

Created as `mapandmuster-db`, database name `mapandmuster`, major version 17.

- Internal URL: used by `mapandmuster-api` automatically.
- External URL: Dashboard → the database → **Connections**. Use this for
  `eng/run-migrations.*` and `scripts/pg-dump.*` from your laptop.
- Confirm backup/PITR settings in the dashboard (see `docs/database-backup-restore.md`).

Approve the first production migration **before** inviting players. Production does not
migrate on API startup. The next API start still runs identity maintenance: it creates
`rosstheboss` when missing (using `Identity__BootstrapAdminPassword`) and seeds Test 1–Test 30
for administrator impersonation. Those test accounts cannot password-login.

```powershell
./eng/build-migrations.ps1
./eng/run-migrations.ps1 -ConnectionString '<RENDER_DATABASE_URL>'
```

```bash
./eng/build-migrations.sh
./eng/run-migrations.sh '<RENDER_DATABASE_URL>'
```

Use **single quotes**. Do not leave angle brackets around the pasted URL. PowerShell double quotes
expand `$` in Render passwords and will corrupt the string.

The run scripts refuse an empty connection string and convert `postgres://` / `postgresql://` URLs
to Npgsql keyword form (`Host=...;SSL Mode=Require`) before calling the bundle. Prefer additive
schema changes; see expand/contract below. Do not commit the URL.

## API service

- Dockerfile: `src/MapAndMuster.Api/Dockerfile` (context is the repository root).
- Health check: `GET /health` (same checks as `/health/ready`; `/health/live` is process-only).
- Listens on Render `PORT`.
- Persistent disk mounted at `/app/app-data` (`Storage__RootPath`). Uploads are **not**
  object storage yet (`docs/DECISIONS-NEEDED.md` item 18).
- `ForwardedHeaders__Enabled=true` so OAuth redirects use HTTPS behind the proxy. Do not
  expose the container port directly to the internet.
- `Database__ApplyMigrationsOnStartup=false`.

First-boot order: database exists → apply EF bundle → confirm `/health` returns
`{"status":"Healthy"}`.

## Angular static site

Build command in the Blueprint:

```text
npm --prefix src/MapAndMuster.Web ci && npm --prefix src/MapAndMuster.Web run build
```

Publish directory: `src/MapAndMuster.Web/dist/mapandmuster-web/browser`.

`/*` rewrites to `/index.html` for SPA routes. `config.json` stays public and must not
contain secrets. Leave `apiBaseUrl` empty so the browser calls same-origin `/api`.

Render static sites do **not** reverse-proxy `/api` to the API. Cookie authentication
requires same-origin `/api`. Cloudflare must route `/api` and `/health` to `mapandmuster-api`
and everything else to `mapandmuster-web`. See **Domain and DNS** below.

## Environment variables

Full table: `docs/environments.md`. Secret handling: `docs/secrets.md`.

Production and Staging share the same required-key validation. Staging must use a
different database, Resend key, From address, OAuth clients, and a hostname with a
`staging` DNS label (`https://staging.mapandmuster.com`). The API refuses a Production origin
that contains a `staging` label, and a Staging origin that does not.

## Health checks

| Path | Meaning |
|---|---|
| `GET /health/live` | Process is running |
| `GET /health/ready` | PostgreSQL is reachable when a connection string is configured |
| `GET /health` | Same checks as ready (Render and load balancers) |

Responses are `{"status":"Healthy"}` or an equivalent status string. They omit connection
strings, exceptions, and check details. Unhealthy ready checks use HTTP 503.

After Cloudflare is configured, probe `https://mapandmuster.com/health`. Directly,
`https://<API_RENDER_HOST>/health` also works.

## Deploy-after-CI

`autoDeployTrigger: checksPass` on both services. Confirm in each service's **Settings →
Build & Deploy** that deploys wait for GitHub checks. Do not enable deploy-on-every-push.

The smoke workflow does not deploy. Run it from **Actions → Smoke test → Run workflow**
after a release.

## Domain and DNS

Cookie auth assumes the browser origin is `https://mapandmuster.com` and that `/api` is on
that same origin.

Recommended shape:

```text
https://mapandmuster.com/              → mapandmuster-web
https://mapandmuster.com/api/*         → mapandmuster-api
https://mapandmuster.com/health        → mapandmuster-api
https://mapandmuster.com/health/*      → mapandmuster-api
https://api.mapandmuster.com/          → mapandmuster-api (optional, debugging only)
```

`api.mapandmuster.com` alone is **not** enough for cookies. Do not split the Angular origin
from `/api` until CORS and cookie `SameSite` work is explicitly added.

### Cloudflare

1. Add `mapandmuster.com` to Cloudflare and complete nameserver delegation.
2. SSL/TLS mode **Full (strict)** once Render certificates exist.
3. Create a Worker (or equivalent reverse-proxy rule) that forwards by path:

```javascript
export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    const api = new URL(env.API_ORIGIN);
    const web = new URL(env.WEB_ORIGIN);
    const path = url.pathname;
    const useApi =
      path === '/health' ||
      path.startsWith('/health/') ||
      path.startsWith('/api/');
    const origin = useApi ? api : web;
    url.protocol = 'https:';
    url.hostname = origin.hostname;
    const headers = new Headers(request.headers);
    headers.set('X-Forwarded-Host', request.headers.get('Host') ?? '');
    headers.set('X-Forwarded-Proto', 'https');
    return fetch(url, { method: request.method, headers, body: request.body, redirect: 'manual' });
  },
};
```

Set Worker secrets/vars `API_ORIGIN=https://<API_RENDER_HOST>` and
`WEB_ORIGIN=https://<WEB_RENDER_HOST>` (include `https://`, no trailing path).

4. Route `https://mapandmuster.com/*` to that Worker.
5. Optional: CNAME `api` → `<API_RENDER_HOST>` for direct health checks.
6. `PublicWeb__Origin` on the API must be `https://mapandmuster.com` (no trailing slash).

Confirm HTTPS in the browser with no certificate warnings.

## Email (Resend)

1. Create a Resend account and API key. Store it only in Render (`Email__Resend__ApiKey`).
2. Add and verify `mapandmuster.com` (or the subdomain you send from).
3. Add the SPF, DKIM, and DMARC records Resend shows. Those values come from Resend, not
   from this repository.
4. Set `Email__FromAddress` to an address on that domain.
5. Send a test: register a user and confirm the message arrives. Local Mailpit remains for
   development (`docs/SETUP.md`).

## OAuth

Optional. Console steps and callback paths: `docs/authentication-production.md`.

Production redirect URIs (same-origin through Cloudflare):

```text
https://mapandmuster.com/api/auth/external/google/callback
https://mapandmuster.com/api/auth/external/facebook/callback
https://mapandmuster.com/api/auth/external/discord/callback
```

Add the corresponding Client ID/Secret or App ID/Secret on `mapandmuster-api`. Do not put them
in Angular or `config.json`.

## Smoke testing

Does not log in and does not use real OAuth credentials.

```powershell
./scripts/smoke-test.ps1 -FrontendUrl "https://mapandmuster.com"
```

```bash
FRONTEND_URL=https://mapandmuster.com ./scripts/smoke-test.sh
```

Or GitHub **Actions → Smoke test**, input `frontend_url=https://mapandmuster.com`. Leave
`api_url` blank when `/health` is same-origin.

Checks: `/` contains `app-root`, `/health/live` and `/health` return `{"status":"Healthy"}`.

Then manually: sign up with email, confirm the message, sign in, and (if configured) try
each OAuth provider.

## Rollback

1. In Render, open the service → **Events** / deploys → deploy the previous successful
   image or commit.
2. Do **not** roll back a destructive migration. Restore onto a **non-production**
   database first (`docs/database-backup-restore.md`), then decide.
3. Keep `Database__ApplyMigrationsOnStartup=false` so a rolled-back image cannot apply a
   newer schema by accident.

## Backups

Verify Render's managed backup/PITR **before** a real campaign starts. Take an independent
`pg_dump` and restore it onto staging or a throwaway database. Details:
`docs/database-backup-restore.md`.

## Staging

Not required for the first bring-up. When you want it, use a **separate** database and
secrets. See `docs/staging.md`.

## Error monitoring

No Sentry (or similar) package is required for the first deployment. Production and
Staging already emit JSON console logs with UTC timestamps, `CorrelationId`, and
`X-Correlation-ID`. ASP.NET Core `ProblemDetails` handles HTTP errors.

A later provider should hook into that logging pipeline with environment variables only.
Do not add a paid APM dependency until there is an ADR.

## Expand/contract migrations

Prefer additive schema changes (new nullable columns, new tables) that the running API can
ignore, then deploy code that uses them, then remove unused columns in a later release.
Avoid one-step renames or destructive drops against a live campaign database.

## Local API container

```bash
docker build -f src/MapAndMuster.Api/Dockerfile -t mapandmuster-api .
docker run --rm -p 8080:8080 --env-file .env -e ConnectionStrings__Campaign="Host=host.docker.internal;Port=5432;Database=mapandmuster;Username=mapandmuster;Password=mapandmuster" mapandmuster-api
```

Use `ASPNETCORE_ENVIRONMENT=Development` or supply Production/Staging variables from
`docs/environments.md`. `docker compose up -d` still starts only PostgreSQL and Mailpit.

## Continuous integration

`.github/workflows/ci.yml` runs on pull requests, pushes to `master`, and `workflow_dispatch`.
It does not deploy.

`.github/workflows/nightly.yml` runs around 02:00 America/Indiana/Indianapolis (06:00 UTC
during EDT) and on `workflow_dispatch`. It reuses `ci.yml` and adds NuGet/npm audits. It
does not deploy.
