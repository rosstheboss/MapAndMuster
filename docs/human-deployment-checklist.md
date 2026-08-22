# Human deployment checklist

Agent-owned repository work for Phases 1–3 is already in Git: Docker, health endpoints,
Resend adapter, CI, `render.yaml`, smoke scripts, and these docs. This file is **your**
work. None of it should be done by an agent in your accounts.

Fill placeholders from `docs/deployment.md`. Do not commit secrets. Where to put each
value is listed next to the step.

The product name is **Map & Muster**. Public origin is `https://mapandmuster.com`.
Render resource names are `mapandmuster-api`, `mapandmuster-web`, and `mapandmuster-db`.

## Already done in the repository (do not redo)

- [x] API Dockerfile, health endpoints, JSON logs, correlation id
- [x] Angular `config.json` `apiBaseUrl` (leave empty for same-origin `/api`)
- [x] Resend + SMTP provider switch (`Email__Provider`)
- [x] OAuth settings externalized (`docs/authentication-production.md`)
- [x] EF migration bundle scripts (`eng/build-migrations.*`, `eng/run-migrations.*`)
- [x] CI on pull requests and `main`; nightly audits; no production deploy
- [x] `render.yaml` (API + static site + Postgres 17; no Worker)
- [x] Smoke scripts and `.github/workflows/smoke-test.yml` (`workflow_dispatch` only)

## Accounts / billing

- [ ] Confirm the GitHub repository and that Actions runs on `main`
      → GitHub → **Actions** → CI must be green
- [ ] Create a Render account/workspace
      → [dashboard.render.com](https://dashboard.render.com)
- [ ] Select a Render plan you accept (managed Postgres is paid)
      → Render → **Billing**
- [ ] Create a Cloudflare account
      → [dash.cloudflare.com](https://dash.cloudflare.com)
- [ ] Confirm `mapandmuster.com` is in Cloudflare (already purchased)
      → Your registrar; then add the zone in Cloudflare
- [ ] Create a Resend account
      → [resend.com](https://resend.com)
- [ ] Optional: Google Cloud project for OAuth
      → [console.cloud.google.com](https://console.cloud.google.com) → **APIs & Services → Credentials**
- [ ] Optional: Discord Developer application
      → [discord.com/developers/applications](https://discord.com/developers/applications)
- [ ] Optional: Meta / Facebook Developer application
      → [developers.facebook.com](https://developers.facebook.com)

## Domain

- [ ] Add `mapandmuster.com` to Cloudflare and change nameservers at the registrar
      → Cloudflare → **Add a domain**; registrar NS panel
- [ ] Create the same-origin reverse proxy (Worker or equivalent) so `/api` and `/health`
      go to `mapandmuster-api` and all other paths go to `mapandmuster-web`
      → Copy the Worker snippet from `docs/deployment.md` → **Domain and DNS**
      → Worker vars: `API_ORIGIN=https://<API_RENDER_HOST>`, `WEB_ORIGIN=https://<WEB_RENDER_HOST>`
- [ ] Route `https://mapandmuster.com/*` to that Worker
- [ ] Optional: CNAME `api.mapandmuster.com` → `<API_RENDER_HOST>` for direct debugging
- [ ] SSL/TLS **Full (strict)** after Render certificates exist
      → Cloudflare → **SSL/TLS**
- [ ] Open `https://mapandmuster.com` and confirm HTTPS with no warnings

## Database

- [ ] Apply `render.yaml` **or** create Postgres 17 in Render and bind it as
      `ConnectionStrings__Campaign`
      → Render → **Blueprints → New** → this repo, file `render.yaml`
      → Confirm **region `ohio`** (immutable) or edit the file first
- [ ] Copy the **External** database URI somewhere safe (password manager), not Git
      → Render → `mapandmuster-db` → **Connections**
- [ ] Confirm managed backup / PITR retention you accept
      → Render → `mapandmuster-db` → backups. Details: `docs/database-backup-restore.md`
- [ ] Build the EF bundle on a machine with the .NET SDK
      → `./eng/build-migrations.ps1` or `./eng/build-migrations.sh`
- [ ] Approve and apply the first production migration
      → `./eng/run-migrations.ps1 -ConnectionString "<RENDER_DATABASE_URL>"`
      → Keep `Database__ApplyMigrationsOnStartup=false` (already set in the Blueprint)
- [ ] Set `Identity__BootstrapAdminPassword` on `mapandmuster-api` (Blueprint `sync: false`)
      → Password manager; 12+ characters with upper, lower, digit, and special
      → Used only to create `rosstheboss` if that account is missing; change the password in-app after first login
- [ ] After the API is up, sign in as `ross.gustafson@gmail.com` / `rosstheboss` and confirm Test 1–Test 30 appear under Test users

## Email

- [ ] Add and verify the sending domain in Resend
      → Resend → **Domains**
- [ ] Add the SPF record Resend displays
      → Cloudflare → **DNS**
- [ ] Add the DKIM record(s) Resend displays
      → Cloudflare → **DNS**
- [ ] Add a DMARC record (Resend documents a starting policy)
      → Cloudflare → **DNS**
- [ ] Create a Resend API key
      → Resend → **API Keys** → store as `Email__Resend__ApiKey` on `mapandmuster-api`
      → Key name in Render: `Email__Resend__ApiKey` (`docs/environments.md`, `docs/secrets.md`)
- [ ] Set `Email__FromAddress` to an address on that domain
      → Render → `mapandmuster-api` → **Environment**
- [ ] Optionally change `Email__FromName` from `Map & Muster`
- [ ] Send a test: register on the site and confirm the inbox (or Resend logs)

## Google OAuth (optional)

- [ ] Create a **Web application** OAuth client
      → Google Cloud → **Credentials**
- [ ] Authorized JavaScript origins: `http://localhost:4200` and `https://mapandmuster.com`
- [ ] Redirect URI: `https://mapandmuster.com/api/auth/external/google/callback`
      → Exact list: `docs/authentication-production.md`
- [ ] Add `Authentication__Google__ClientId` on `mapandmuster-api`
- [ ] Add `Authentication__Google__ClientSecret` on `mapandmuster-api`
- [ ] Test production Google login

## Discord OAuth (optional)

- [ ] Create/configure an application
      → Discord Developer Portal
- [ ] Redirect URI: `https://mapandmuster.com/api/auth/external/discord/callback`
- [ ] Add `Authentication__Discord__ClientId` on `mapandmuster-api`
- [ ] Add `Authentication__Discord__ClientSecret` on `mapandmuster-api`
- [ ] Test production Discord login

## Facebook OAuth (optional)

- [ ] Create a Meta app and add **Facebook Login**
      → Meta Developer dashboard
- [ ] Valid OAuth redirect URI: `https://mapandmuster.com/api/auth/external/facebook/callback`
- [ ] Add `Authentication__Facebook__AppId` on `mapandmuster-api`
- [ ] Add `Authentication__Facebook__AppSecret` on `mapandmuster-api`
- [ ] Complete app-mode / review steps Meta requires before public use
- [ ] Test production Facebook login

## Deployment

- [ ] Connect Render to this GitHub repository (Blueprint flow does this)
- [ ] Approve service creation and the Postgres plan
- [ ] Enter `PublicWeb__Origin=https://mapandmuster.com` (no `staging` label)
      → Render → `mapandmuster-api` → **Environment**
- [ ] Enter remaining secrets listed above; never put them in `config.json` or Git
- [ ] Confirm CI is green on the commit you deploy
      → GitHub → **Actions**
- [ ] Confirm auto-deploy is **Deploy only if checks pass**
      → Render service → **Settings**; already `checksPass` in `render.yaml`
- [ ] Wait for `mapandmuster-api` and `mapandmuster-web` to deploy
- [ ] `GET https://mapandmuster.com/health` (and `/health/live`) return Healthy
- [ ] Open `https://mapandmuster.com/` and see the Angular app
- [ ] Confirm `/api` is same-origin (browser network tab: API calls stay on `mapandmuster.com`)
- [ ] Confirm Postgres connectivity (`/health` ready check is enough)
- [ ] There is no Worker to start; outbox runs inside the API process
- [ ] Verify email (registration confirmation)
- [ ] Verify each enabled OAuth provider
- [ ] Run smoke tests
      → `./scripts/smoke-test.ps1 -FrontendUrl "https://mapandmuster.com"`
      → or GitHub **Actions → Smoke test → Run workflow**
- [ ] Take a `pg_dump` and restore it onto a **non-production** database before a real campaign
      → `docs/database-backup-restore.md`

## Staging (optional, later)

- [ ] Follow `docs/staging.md` (separate database, secrets, OAuth apps, and
      `https://staging.mapandmuster.com`)
- [ ] Never paste the production `ConnectionStrings__Campaign` into staging
