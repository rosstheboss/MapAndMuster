# Staging

Staging is optional for the first public bring-up. The repository is ready for it: Staging
is a first-class ASP.NET environment (`appsettings.Staging.json`) with the same required
settings as Production, and the API refuses configuration that looks like the other slot.

Do not create staging resources in the production Blueprint. A second Render environment
(or a second Blueprint apply with different service names) keeps billing and secrets
isolated.

## Hostnames

```text
https://staging.mapandmuster.com          public staging site (Cloudflare → staging static site)
https://staging.mapandmuster.com/api/*    staging API (same-origin, like production)
https://mapandmuster.com                  production (never shared with staging)
```

`PublicWeb__Origin` for Staging **must** include a `staging` DNS label
(`https://staging.mapandmuster.com` is valid; `https://mapandmuster.com` is rejected). Production
**must not** use a `staging` label. That check exists so a copied origin cannot silently
point players at the wrong slot. It is not a substitute for separate databases.

`api-staging.mapandmuster.com` is optional, for direct API debugging only. Cookies still
require same-origin `/api` on `https://staging.mapandmuster.com`.

## Isolation rules

Staging must have all of the following, none shared with production:

| Concern | Staging | Production |
|---|---|---|
| PostgreSQL | Own Render database | `mapandmuster-db` |
| `ConnectionStrings__Campaign` | Staging URI only | Production URI only |
| Resend API key | Staging key or test domain | Production key |
| `Identity__BootstrapAdminPassword` | Staging-only bootstrap password | Production-only bootstrap password |
| `Email__FromAddress` | Address on the staging/test domain | Production From address |
| OAuth clients | Separate redirect URIs on `staging.mapandmuster.com` | URIs on `mapandmuster.com` |
| Uploads disk | Own disk | `mapandmuster-uploads` |
| `ASPNETCORE_ENVIRONMENT` | `Staging` | `Production` |

The API cannot see the other environment's connection string. Isolation is an operator
duty. The process will not fall back to `appsettings.json` production credentials because
those keys are empty there; it **will** use whatever you paste into Render.

Never copy production `ConnectionStrings__Campaign` into a staging service. Never point
`scripts/pg-restore.*` at production.

## Suggested Render layout

Create a second Blueprint instance or duplicate services named with a `staging-` prefix,
for example `staging-mapandmuster-db`, `staging-mapandmuster-api`, `staging-mapandmuster-web`. Bind
the staging API only to the staging database.

Set:

```text
ASPNETCORE_ENVIRONMENT=Staging
PublicWeb__Origin=https://staging.mapandmuster.com
Email__Provider=Resend
Identity__BootstrapAdminPassword=<STAGING_BOOTSTRAP_ADMIN_PASSWORD>
Database__ApplyMigrationsOnStartup=false
ForwardedHeaders__Enabled=true
```

Apply migrations with the **staging** external URI:

```powershell
./eng/run-migrations.ps1 -ConnectionString '<STAGING_RENDER_DATABASE_URL>'
```

Cloudflare: a second Worker route for `staging.mapandmuster.com`, or the same Worker with
host-based branching. Do not reuse production Worker `API_ORIGIN` / `WEB_ORIGIN`.

## OAuth redirect URIs (staging)

```text
https://staging.mapandmuster.com/api/auth/external/google/callback
https://staging.mapandmuster.com/api/auth/external/facebook/callback
https://staging.mapandmuster.com/api/auth/external/discord/callback
```

Use a separate OAuth client (or at least extra redirect URIs) so a staging redirect cannot
complete against production.

## Smoke and backups

```powershell
./scripts/smoke-test.ps1 -FrontendUrl "https://staging.mapandmuster.com"
```

Restore production dumps onto staging when testing backups
(`docs/database-backup-restore.md`). That is one of the reasons staging exists.
