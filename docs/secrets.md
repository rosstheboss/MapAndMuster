# Secrets

Secrets belong in user secrets, a gitignored `.env` file, or the host's secret store. They must not
appear in Git, Dockerfiles, images, logs, health responses, or Angular source.

## Never commit

- Database passwords and production connection strings
- SMTP passwords
- Resend API keys
- OAuth client secrets and app secrets
- `Identity:BootstrapAdminPassword`
- `Identity:BootstrapAdminEmail` when it is a personal mailbox
- Cookie/signing keys if they are ever rotated out of the framework defaults
- Email confirmation and password-reset tokens
- Player email addresses in fixtures or logs

`.gitignore` already excludes `.env` and `.env.*` while tracking `.env.example`.

## Local development

Preferred options:

1. `dotnet user-secrets` on `src/MapAndMuster.Api` (user secrets id `mapandmuster-api-development`)
2. A gitignored `.env` copied from `.env.example`

`docker-compose.yml` uses the well-known local password `campaign`. That value is for the developer
workstation only.

## Production and staging

Enter secrets in the host (Render Dashboard environment variables, including Blueprint
`sync: false` prompts). Do not put them in `render.yaml`. Staging must use a different
PostgreSQL database, Resend key, From address, and OAuth clients than production.

Startup validation in Production and Staging names missing keys such as
`ConnectionStrings:Campaign`, `Email:Resend:ApiKey`, `Identity:BootstrapAdminPassword`, and
`Identity:BootstrapAdminEmail`.
It never prints the values.

## Logging

Do not log:

- passwords
- access or refresh tokens
- OAuth authorization codes
- cookies
- email verification or password-reset tokens
- database credentials
- API keys

Correlation identifiers and request ids are safe to log. Outbox delivery failures record the
message id, not the payload.
