# ADR 0003: Initial Production Hosting Stack

- Status: Accepted
- Date: 2026-08-22

## Context

`docs/DECISIONS-NEEDED.md` item 18 asked for production hosting, object storage, email provider,
and background-job mechanism. `docs/AGENT_DEPLOYMENT_PLAN.md` is the operator direction for the
first production topology. ADR 0001 already deploys only the API and Angular applications.

## Decision

Prepare the repository for:

- GitHub and GitHub Actions
- Render for the API container, Angular static site, and managed PostgreSQL
- Cloudflare for DNS
- Resend for transactional email
- Optional Google, Facebook, and Discord login already decided in ADR 0002

Keep background work in `Campaign.Api` hosted services (email outbox). Do not add a Worker
project, object-storage provider, or extra runtime until a later ADR.

## Consequences

- Phase 1 can produce Docker images, health endpoints, and configuration docs without cloud accounts.
- Production email is selected through `Email:Provider=Resend`; local Mailpit SMTP remains.
- Operators must reverse-proxy `/api` onto the Angular origin for cookie authentication.
- Object storage remains local disk (`Storage:RootPath`) until a later decision.
