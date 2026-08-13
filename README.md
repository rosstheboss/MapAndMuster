# Campaign Map Application - AI Development Template

This repository is a generic, MIT-licensed map-campaign application built with .NET 10/C# 14,
Angular 22, and PostgreSQL.

It is designed for use with multiple coding assistants. `AGENTS.md` is the canonical source;
provider-specific files are intentionally thin adapters.

## Solution

```text
Campaign.sln
src/
  Campaign.Domain/
  Campaign.Application/
  Campaign.Infrastructure/
  Campaign.Api/
  Campaign.Web/
tests/
  Campaign.Backend.UnitTests/
  Campaign.Api.IntegrationTests/
  Campaign.Web.E2E/
docs/
```

Only `Campaign.Api` and `Campaign.Web` are deployed applications. The other production
projects are class libraries that enforce architectural boundaries.

See `docs/SETUP.md` for prerequisites, local services, and verification. Review
`docs/DECISIONS-NEEDED.md` before implementing unresolved campaign rules.

## Canonical documents

- `AGENTS.md`: mandatory agent workflow and repository-wide constraints.
- `docs/PRODUCT.md`: product boundaries and users.
- `docs/DOMAIN.md`: campaign language, state, and invariants.
- `docs/ARCHITECTURE.md`: project responsibilities and dependency rules.
- `docs/CAMPAIGN-RULES-MATRIX.md`: classification of the supplied campaign draft.
- `docs/CODING-STANDARDS.md`: human-readable style policy.
- `docs/TESTING-STRATEGY.md`: required test levels and scenarios.
- `docs/SECURITY.md`: secrecy, authorization, upload, and audit requirements.
- `docs/DECISIONS-NEEDED.md`: unresolved product rules.
