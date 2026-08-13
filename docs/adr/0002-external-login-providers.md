# ADR 0002: Optional External Login Providers

- Status: Accepted
- Date: 2026-08-13

## Context

Players asked to sign up with Google, Facebook, and Discord in addition to email and
password. `AGENTS.md` requires an explicit decision before adding external identity
providers. Operator credentials are not available in source control, and Facebook/Google/
Discord apps must be created in each environment.

## Decision

Keep ASP.NET Core Identity email/password as the primary authentication method.

Register Google, Facebook, and Discord as optional, configuration-gated authentication
handlers. A provider appears in the UI only when both a client id and secret are
configured through user secrets or environment variables.

New external sign-ins may create an account after the player supplies a unique username and
location. Name and avatar are imported from the provider when present and can be edited
later. If the provider email already belongs to a local account, the application does not
silently merge identities; the player must sign in to the existing account.

## Consequences

- Local development and CI work without OAuth apps.
- Production operators must register callback URLs under `/api/auth/external/{provider}/callback`.
- Discord uses the built-in OAuth handler rather than a third-party package.
- Linking an external login onto an already-authenticated account remains future work.
