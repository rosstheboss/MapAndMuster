# Authentication in production

Google, Facebook, and Discord are optional. A provider appears in the UI only when both of its
credentials are configured. Email and password remain the primary sign-in method. See
`docs/adr/0002-external-login-providers.md`.

Do not create provider applications from this repository. Register them in each provider console,
then store credentials as environment variables.

## Configuration keys

| Provider | Id | Secret |
|---|---|---|
| Google | `Authentication__Google__ClientId` | `Authentication__Google__ClientSecret` |
| Facebook | `Authentication__Facebook__AppId` | `Authentication__Facebook__AppSecret` |
| Discord | `Authentication__Discord__ClientId` | `Authentication__Discord__ClientSecret` |

Callback paths (scheme and host come from the incoming request, including forwarded headers):

| Provider | Path |
|---|---|
| Google | `/api/auth/external/google/callback` |
| Facebook | `/api/auth/external/facebook/callback` |
| Discord | `/api/auth/external/discord/callback` |

After the provider returns, the API finishes at `/api/auth/external/callback` and redirects the
browser to `PublicWeb:Origin`.

## Example URLs

Replace placeholders. Prefer the public site origin so cookies stay first-party:

```text
https://<ROOT_DOMAIN>/api/auth/external/google/callback
https://<ROOT_DOMAIN>/api/auth/external/facebook/callback
https://<ROOT_DOMAIN>/api/auth/external/discord/callback
```

Local development (Angular proxy):

```text
http://localhost:4200/api/auth/external/google/callback
http://localhost:4200/api/auth/external/facebook/callback
http://localhost:4200/api/auth/external/discord/callback
```

If the API is reached only at `https://<API_DOMAIN>`, register that host instead and understand that
cookie authentication across two origins is not configured in this phase.

## Scopes requested

| Provider | Scopes / fields |
|---|---|
| Google | ASP.NET Core defaults: `openid`, `profile`, `email` |
| Facebook | `email`, plus fields `first_name`, `last_name`, `picture` |
| Discord | `identify`, `email` |

A matching email does not auto-link to an existing local account.

## Provider console steps (human)

### Google

1. Create an OAuth client of type **Web application** in a Google Cloud project.
2. Add authorized JavaScript origins for `http://localhost:4200` and `https://<ROOT_DOMAIN>`.
3. Add the exact redirect URIs above.
4. Copy the client id and secret into server-side configuration.

### Discord

1. Create an application in the Discord developer portal.
2. Add the exact redirect URI.
3. Copy the client id and secret into server-side configuration.

### Facebook

1. Create a Meta app and add **Facebook Login**.
2. Add the exact redirect URI and the production domain.
3. Copy the app id and secret into server-side configuration.
4. Complete any required app-mode or review steps before public use.

## Forwarded headers

Production and Staging consume `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` so
OAuth redirects use HTTPS behind Render or another proxy. That trust model assumes the container
port is not exposed directly to the internet. Override with `ForwardedHeaders__Enabled`.
