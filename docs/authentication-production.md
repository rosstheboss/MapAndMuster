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
https://mapandmuster.com/api/auth/external/google/callback
https://mapandmuster.com/api/auth/external/facebook/callback
https://mapandmuster.com/api/auth/external/discord/callback
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

Public privacy URL for OAuth consoles: `https://mapandmuster.com/privacy`.

## Provider console steps (human)

### Google

1. Create an OAuth client of type **Web application** in a Google Cloud project.
2. Add authorized JavaScript origins for `http://localhost:4200` and `https://mapandmuster.com`.
3. Add the exact redirect URIs above.
4. Copy the client id and secret into `Authentication__Google__ClientId` and
   `Authentication__Google__ClientSecret` on the API service.
5. Open **Google Auth Platform → Branding** (or **APIs & Services → OAuth consent screen**).
   Set the app name, support email, developer contact, authorized domain `mapandmuster.com`,
   and privacy policy `https://mapandmuster.com/privacy`.
6. Publish the consent screen to **In production**. Leave the app in **Testing** and Google
   shows **Go to [app] (unsafe)** to every non-test user. That interstitial is Google's
   unpublished-app warning, not a Chrome Safe Browsing or HTTPS failure. `openid`, `email`,
   and `profile` are non-sensitive scopes and usually do not need a full verification review
   once the screen is published.

### Discord

1. Open [discord.com/developers/applications](https://discord.com/developers/applications)
   and create an application.
2. Open **OAuth2**. Add redirect `https://mapandmuster.com/api/auth/external/discord/callback`
   (and the localhost URI if you will test locally).
3. Copy **Client ID** and **Client Secret** into `Authentication__Discord__ClientId` and
   `Authentication__Discord__ClientSecret` on the API service. Saving those env vars
   redeploys the API; the Discord button appears after that.
4. Discord does not need a branded verification step for `identify` and `email`.

### Facebook

1. Open [developers.facebook.com](https://developers.facebook.com) and create an app.
2. Add the **Facebook Login** product. Under Facebook Login settings, add Valid OAuth
   Redirect URI `https://mapandmuster.com/api/auth/external/facebook/callback`.
3. In app settings, set the privacy policy URL to `https://mapandmuster.com/privacy` and
   add the production domain.
4. Copy **App ID** and **App Secret** into `Authentication__Facebook__AppId` and
   `Authentication__Facebook__AppSecret` on the API service.
5. Switch the Meta app to **Live**. `email` is a standard permission; complete any extra
   review Meta shows before public use.

## Forwarded headers

Production and Staging consume `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` so
OAuth redirects use HTTPS behind Render or another proxy. That trust model assumes the container
port is not exposed directly to the internet. Override with `ForwardedHeaders__Enabled`.
