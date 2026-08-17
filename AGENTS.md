# Repository Instructions for AI Coding Agents

## Authority and scope

This is the canonical repository-wide instruction file. Nested `AGENTS.md` files add
area-specific requirements. If instructions conflict, follow the nearest applicable file,
then this file, then provider-specific adapter files.

Read the relevant documents under `docs/` before changing behavior. Never infer a campaign
rule from theme, lore, or familiarity with a tabletop game. Implement only documented rules.
Unresolved behavior belongs in `docs/DECISIONS-NEEDED.md` and requires user direction.

## Technical baseline

- .NET 10 LTS and C# 14; no preview framework or language features.
- Angular 22 with standalone components, zoneless change detection, strict TypeScript, and
  strict template checking.
- PostgreSQL through Entity Framework Core.
- CSS, with no assumed component library.
- ASP.NET Core Identity with email/password authentication and replaceable external-login
  integrations.
- xUnit, `WebApplicationFactory`, disposable PostgreSQL integration databases, Vitest, and
  Playwright.

## Architecture

- `Campaign.Domain` contains pure campaign rules and has no infrastructure dependencies.
- `Campaign.Application` contains use cases and ports; it depends only on Domain.
- `Campaign.Infrastructure` implements persistence, identity, email, storage, time, and jobs.
- `Campaign.Api` is the HTTP composition root and executable backend.
- `Campaign.Web` communicates with the backend only through its documented API.
- Do not bypass project boundaries for convenience. See `docs/ARCHITECTURE.md`.

## Mandatory workflow

1. State the behavior being changed and identify applicable domain/security rules.
2. Inspect existing code and tests before editing.
3. Make the smallest cohesive change that satisfies the request.
4. Add or update tests in the same change.
5. Update documentation when behavior, contracts, setup, or architectural decisions change.
6. Run the narrowest relevant checks during development and the complete verification suite
   before reporting completion.
7. Report commands run, results, and any checks not run.

## Quality gates

Run from the repository root:

```bash
dotnet restore Campaign.sln
dotnet format Campaign.sln --verify-no-changes --no-restore
dotnet build Campaign.sln --configuration Release --no-restore
dotnet test Campaign.sln --configuration Release --no-build
npm --prefix src/Campaign.Web ci
npm --prefix src/Campaign.Web run verify
npm --prefix tests/Campaign.Web.E2E ci
npm --prefix tests/Campaign.Web.E2E test
```

Use repository scripts when present. Never weaken analyzers, lint rules, compiler strictness,
or tests merely to make a check pass. A suppression requires a narrow scope and explanation.

## Testing obligations

- Every behavioral change requires tests at the lowest effective level.
- Bug fixes should begin with a failing regression test when practical.
- Domain rules require isolated unit tests.
- Persistence, authorization, serialization, API, notification-outbox, and concurrency behavior
  require integration tests.
- Critical multi-user workflows and secrecy boundaries require Playwright coverage.
- Tests must cover success, invalid input, unauthorized access, boundary times, and concurrency
  where applicable.
- Never delete, skip, loosen, or rewrite an assertion simply because generated code fails it.

## Domain and security invariants

- The server clock and database state are authoritative; timestamps are stored in UTC.
- Hidden orders, relics, and private objectives must not be sent to unauthorized clients.
- A player may uncommit only while the planning window remains open.
- The final required commitment closes planning atomically; otherwise the deadline closes it.
- Missing action slots become `Hold`; invalid orders resolve as documented rather than being
  silently repaired.
- GM inspection and intervention are explicit, immutable, attributed, and notified in-app and
  by email.
- Never overwrite an original order, result submission, or audit event.
- Authorization is permission-based and campaign-scoped. A GM may also be a player.
- Actual actor and effective actor are recorded separately when staff act for another party.
- Campaign mutations use optimistic concurrency and a campaign revision.
- Email notifications use a transactional outbox; state changes do not depend on synchronous
  email delivery.
- User-provided HTML and SVG are untrusted. Do not render unsanitized active content.

## Repository and intellectual property

- Keep bundled code, fixtures, copy, maps, factions, and artwork generic and fictional.
- Do not reproduce proprietary rules text, logos, book scans, or game artwork.
- Runtime administrators may supply their own text and images subject to application policy.
- Never commit secrets, production data, private objectives, unrevealed relic locations, or
  player email addresses.
- Preserve the MIT license and authorship notice.

## Code style

Follow `.editorconfig`, analyzers, ESLint, Prettier, and Stylelint. Key defaults:

- C#: four spaces, Allman braces, file-scoped namespaces, nullable enabled, async suffixes,
  cancellation propagation, and `var` when the type is evident.
- TypeScript/Angular: two spaces, single quotes, semicolons, trailing commas, 120-character
  width, no unexplained `any`, accessible controls, and thin components.
- Prefer clear domain terms over abbreviations. `CampaignPoint` and `SupplyPoint` are distinct.
- Comments explain intent and exceptional rules, not obvious syntax.

## Change boundaries

- Do not add a component library, state-management framework, job scheduler, cloud provider,
  or external identity provider without an explicit decision.
- Do not implement incomplete castle, army-of-infamy, or unspecified scoring rules.
- Do not use full event sourcing without a recorded architecture decision.
- Do not edit generated API clients directly; regenerate them from OpenAPI.
