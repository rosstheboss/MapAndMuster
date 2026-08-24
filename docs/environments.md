# Environments

The same API binaries and Angular build are configured through environment variables. ASP.NET Core
hierarchical keys use a double underscore, for example `ConnectionStrings__Campaign`.

## Environments

| Name | Typical use |
|---|---|
| `Development` | Local `dotnet run` and Docker Compose PostgreSQL/Mailpit |
| `Testing` | Integration tests |
| `Staging` | Isolated cloud environment with its own database and secrets |
| `Production` | Public campaign |

`Production` and `Staging` share the same required-settings validation. Staging must not reuse the
production connection string, email API key, or OAuth clients.

`PublicWeb__Origin` for Staging must include a `staging` DNS label (`https://staging.mapandmuster.com`).
Production must not use that label. See `docs/staging.md`.

## Backend variables

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Host environment name |
| `PORT` | Listen port in containers (Render). Defaults to `8080` |
| `ConnectionStrings__Campaign` | PostgreSQL connection string. Render injects a `postgres://` URI; the API converts it to Npgsql keyword form and sets `GSS Encryption Mode=Disable`. Local and CI use `Host=...;Database=...`. |
| `PublicWeb__Origin` | Public Angular origin used in email links and OAuth return URLs |
| `Storage__RootPath` | Uploaded-file root, outside web root |
| `Database__ApplyMigrationsOnStartup` | When `true`, the API applies EF migrations on start |
| `ForwardedHeaders__Enabled` | Consume `X-Forwarded-*` from the platform proxy |
| `Email__Provider` | `Smtp` or `Resend` |
| `Email__FromAddress` | From address |
| `Email__FromName` | Optional display name |
| `Email__SmtpHost` | SMTP host (Mailpit `localhost` in Development) |
| `Email__SmtpPort` | SMTP port |
| `Email__SmtpUsername` | Optional SMTP username |
| `Email__SmtpPassword` | Optional SMTP password |
| `Email__EnableSsl` | SMTP SSL/STARTTLS |
| `Email__Resend__ApiKey` | Resend API key |
| `Identity__BootstrapAdminPassword` | Password used only to create `rosstheboss` when that account is missing. Never overwrites an existing password |
| `Authentication__Google__ClientId` | Google OAuth client id |
| `Authentication__Google__ClientSecret` | Google OAuth client secret |
| `Authentication__Facebook__AppId` | Facebook app id |
| `Authentication__Facebook__AppSecret` | Facebook app secret |
| `Authentication__Discord__ClientId` | Discord client id |
| `Authentication__Discord__ClientSecret` | Discord client secret |

Empty OAuth values disable that provider. Do not put production values in `appsettings.json`.

## Angular

Development uses the Angular proxy: browser calls `/api` on `http://localhost:4200`, which forwards
to `http://localhost:5219`.

Production should keep that same-origin `/api` shape behind Cloudflare or another reverse proxy.
`src/MapAndMuster.Web/public/config.json` may set `apiBaseUrl` to an absolute `http(s)` origin when the
same build is pointed at a different API. Cookie authentication still expects the browser and API
to share an origin unless CORS is added later.

`config.json` is public. It must never contain secrets.

## Local workflow

1. `docker compose up -d`
2. Copy `.env.example` to `.env` or set user secrets on `MapAndMuster.Api`
3. Run the API **http** profile on `http://localhost:5219`
4. `npm --prefix src/MapAndMuster.Web start`

See `docs/SETUP.md` and `docs/secrets.md`. Cloud bring-up is `docs/deployment.md` and
`docs/human-deployment-checklist.md`.
