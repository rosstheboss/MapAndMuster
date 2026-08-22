# Old World Campaign — Deployment & Production Readiness Agent Plan

## Purpose

This document is an implementation specification for an AI coding agent working in the existing **Old World Campaign** repository.

The agent should implement **all repository, code, configuration-template, CI/CD, validation, documentation, and local testing work that does not require human-owned credentials, billing decisions, domain ownership, OAuth console access, or production approval**.

The human developer will perform account creation, secret entry, domain/DNS configuration, OAuth provider configuration, production resource creation where necessary, and final deployment approval.

This plan is intentionally staged so deployment can be tested incrementally.

---

# 1. Agent Operating Rules

## 1.1 General Rules

The agent should:

- Inspect the existing repository before making structural assumptions.
- Preserve the current architecture unless a change is necessary for deployment.
- Prefer small, reviewable changes over a large rewrite.
- Reuse existing abstractions and configuration conventions.
- Avoid introducing infrastructure or dependencies that are not needed for the initial production deployment.
- Keep local development working after every stage.
- Keep secrets out of source control.
- Add documentation for every new deployment requirement.
- Run relevant builds/tests after changes.
- Report any blockers that require human action instead of inventing credentials or external configuration.
- Do not perform destructive production/database operations without explicit human approval.
- Do not commit generated secrets, OAuth credentials, connection strings, API keys, or private certificates.

## 1.2 Target Production Stack

Unless the existing repository makes this impractical, prepare the application for the following deployment model:

- **Source control / CI:** GitHub + GitHub Actions
- **Hosting:** Render
- **Frontend:** Angular static site
- **Backend:** ASP.NET Core API running in Docker
- **Background processing:** .NET Worker running as a Render Background Worker
- **Database:** Render Managed PostgreSQL
- **DNS / Domain:** Cloudflare
- **Transactional email:** Resend
- **Authentication:** Google, Discord, Facebook
- **Local development database:** Existing Dockerized PostgreSQL
- **Local development mail:** Existing local mail testing service

The agent must not require the human developer to provision these external services until repository preparation is complete.

---

# 2. Existing Project Deployment Model

Inspect the actual solution and confirm the equivalent of the following projects:

```text
OldWorldCampaign.sln

OldWorldCampaign.Api
OldWorldCampaign.Application
OldWorldCampaign.Domain
OldWorldCampaign.Infrastructure
OldWorldCampaign.Worker
old-world-campaign-web/
```

Do not treat class-library projects as individually deployed services.

Expected deployed workloads:

| Repository component | Production deployment |
|---|---|
| Angular application | Static site |
| ASP.NET Core API | Docker web service |
| .NET Worker | Docker background worker |
| PostgreSQL | Managed PostgreSQL |
| Application library | Compiled into executables |
| Domain library | Compiled into executables |
| Infrastructure library | Compiled into executables |
| Unit/integration tests | CI only |

If the repository differs, adapt this model to the actual project structure and document the mapping.

---

# 3. Stage A — Repository Audit

## Agent-owned work

Before making deployment changes:

1. Inspect the solution/project structure.
2. Identify all executable projects.
3. Identify all project references.
4. Identify:
   - database connection configuration
   - Entity Framework Core configuration
   - authentication configuration
   - email abstraction/implementation
   - background worker configuration
   - logging configuration
   - Angular environment/API URL handling
5. Locate all existing Dockerfiles and Docker Compose files.
6. Locate all tests.
7. Identify any secrets or environment-specific values currently committed to the repository.
8. Identify hard-coded localhost URLs or development-only assumptions.
9. Identify current ports.
10. Identify current database migration strategy.

Create or update:

```text
docs/deployment-audit.md
```

Include:

- actual project structure
- executable services
- deployment risks found
- human actions eventually required
- any deviations from this plan

Do not change architecture merely because the names differ from this document.

---

# 4. Stage B — Environment-Safe Configuration

## Goal

The same binaries/containers should be configurable for development, staging, and production through environment variables.

## Agent-owned work

Audit configuration access and ensure ASP.NET Core supports environment-based overrides.

Use standard .NET hierarchical environment-variable naming where possible, for example:

```text
ConnectionStrings__DefaultConnection
Authentication__Google__ClientId
Authentication__Google__ClientSecret
Authentication__Discord__ClientId
Authentication__Discord__ClientSecret
Authentication__Facebook__AppId
Authentication__Facebook__AppSecret
Email__Provider
Email__Resend__ApiKey
Email__FromAddress
Frontend__BaseUrl
```

Adapt names to existing configuration objects rather than duplicating configuration systems.

### Requirements

- No production secrets in `appsettings.json`.
- No production secrets in Angular source.
- No secrets in Dockerfiles.
- No secrets in `render.yaml`.
- Development defaults may remain in `appsettings.Development.json` only when they are non-sensitive.
- Add startup validation for required production settings when practical.
- Error messages should name missing configuration keys without printing secret values.

Create:

```text
.env.example
docs/environments.md
docs/secrets.md
```

`.env.example` must contain variable names only or safe examples.

Update `.gitignore` to exclude:

```text
.env
.env.*
```

while preserving intentionally tracked safe example files such as `.env.example`.

If the existing repository already uses another convention, preserve it and document it.

---

# 5. Stage C — Production Dockerfiles

## 5.1 API Dockerfile

Create or update the API Dockerfile using a multi-stage .NET build.

Requirements:

- Use supported .NET SDK/runtime versions matching the project.
- Restore dependencies efficiently.
- Build/publish in Release mode.
- Final image must contain runtime only.
- Run as a non-root user where practical.
- Do not copy unnecessary source files into the final image.
- Bind ASP.NET Core to `0.0.0.0`.
- Respect Render's `PORT` environment variable when present.
- Maintain convenient local Docker execution.

Target location should normally be:

```text
OldWorldCampaign.Api/Dockerfile
```

or the equivalent actual API path.

## 5.2 Worker Dockerfile

Create or update a multi-stage Dockerfile for the Worker.

Target location should normally be:

```text
OldWorldCampaign.Worker/Dockerfile
```

The worker should:

- start directly as the container process
- terminate correctly on SIGTERM
- use the same configuration model as the API
- never require an exposed HTTP port unless the existing design intentionally provides one

## 5.3 Docker Ignore

Add/update root or project `.dockerignore` files as appropriate.

Exclude:

```text
.git
.github
bin
obj
node_modules
dist
coverage
local database data
developer secrets
IDE metadata
```

Do not exclude files needed to restore/build the solution.

## Validation

The agent should build both Docker images locally.

---

# 6. Stage D — Health Checks

## Goal

Provide a production-safe health endpoint for the API.

## Agent-owned work

Implement ASP.NET Core health checks using the existing application conventions.

Recommended endpoint:

```text
GET /health
```

At minimum verify:

- application process is alive

Preferably also verify:

- PostgreSQL connectivity

Do not include:

- secret values
- database connection strings
- sensitive diagnostics
- stack traces

If useful, provide:

```text
/health/live
/health/ready
```

where:

- live = process health
- ready = required dependencies available

If that distinction adds unnecessary complexity, `/health` alone is acceptable.

Document the final endpoint in:

```text
docs/deployment.md
```

---

# 7. Stage E — Structured Logging and Correlation

## Agent-owned work

Ensure production logs are structured and useful.

Log useful identifiers when already available:

- request/correlation ID
- user ID
- campaign ID
- turn ID
- battle ID
- relevant action/result
- exception metadata

Never intentionally log:

- passwords
- access/refresh tokens
- OAuth authorization codes
- cookies
- email verification tokens
- password reset tokens
- database credentials
- API keys
- private secrets

Add or verify request correlation IDs.

Prefer built-in ASP.NET Core logging and existing logging packages before introducing another dependency.

If the application already uses Serilog or equivalent, retain it.

Document log behavior.

---

# 8. Stage F — Email Provider Abstraction

## Goal

Keep existing local mail testing while adding production transactional-email support.

## Agent-owned work

Inspect the existing email implementation.

Prefer an abstraction equivalent to:

```csharp
public interface IEmailSender
{
    Task SendAsync(...);
}
```

Do not create a duplicate interface if one already exists.

Provide implementations equivalent to:

```text
Development/local provider
Production Resend provider
```

Select the provider through configuration and dependency injection.

### Production implementation

Add Resend support using either:

- a maintained official/appropriate .NET package, or
- a small typed `HttpClient` integration

Prefer the simplest implementation compatible with the repository.

Configuration should include values equivalent to:

```text
Email__Provider=Resend
Email__Resend__ApiKey=
Email__FromAddress=
Email__FromName=
```

### Local development

Preserve the existing local mail server workflow.

### Tests

Add unit tests where practical for:

- provider selection
- message construction
- missing configuration behavior

Do not make real external email calls in automated tests.

### Human required later

The human developer will:

- create the Resend account
- verify the domain
- create the API key
- configure SPF/DKIM/DMARC
- enter the production secret
- choose final From addresses

---

# 9. Stage G — External Authentication Readiness

## Goal

Ensure Google, Discord, and Facebook authentication can be configured entirely through external configuration.

## Agent-owned work

Audit current authentication.

For each supported provider:

- remove any hard-coded client IDs/secrets
- load credentials from server-side configuration
- keep secrets out of Angular
- make callback paths clear
- ensure production HTTPS/proxy behavior is supported
- document exact callback URL patterns

Suggested configuration shape:

```text
Authentication__Google__ClientId
Authentication__Google__ClientSecret

Authentication__Discord__ClientId
Authentication__Discord__ClientSecret

Authentication__Facebook__AppId
Authentication__Facebook__AppSecret
```

Adapt to existing options classes.

### Forwarded headers

Because production will run behind a reverse proxy, verify ASP.NET Core correctly processes forwarded headers so OAuth callback URLs use HTTPS.

Configure only trusted/appropriate forwarded-header behavior rather than blindly trusting arbitrary proxies if the framework/setup permits a safer configuration.

### Documentation

Create:

```text
docs/authentication-production.md
```

For each provider document:

- required client/application type
- configuration variable names
- expected callback route
- example production URL using placeholders
- example localhost callback
- minimum scopes requested by the application
- human steps required in each provider console

Do not create provider applications or credentials.

---

# 10. Stage H — Angular Production Configuration

## Agent-owned work

Audit how Angular determines the API URL.

Target behavior:

```text
Development:
http://localhost:<api-port>

Production:
https://api.<domain>
```

Prefer a deployment-friendly solution.

If the current Angular app already uses build-time environment configuration safely, it may remain initially.

If practical without unnecessary complexity, prefer runtime public configuration so the same Angular build can move between environments.

Example runtime values may include:

```json
{
  "apiBaseUrl": "https://api.example.com"
}
```

Never put secrets into Angular runtime or build-time configuration.

### SPA routing

Ensure the deployed static site supports Angular client-side routing and routes unknown paths to `index.html`.

### Production build

Verify:

```bash
npm ci
npm run build
```

or the repository's equivalent production build command.

---

# 11. Stage I — Database Migration Strategy

## Goal

Make database schema deployment repeatable and safe.

## Agent-owned work

Audit existing EF Core migrations.

Ensure:

- migrations are committed
- a clean database can be migrated from zero
- CI can validate migrations
- production deployment can invoke migrations without requiring a developer workstation

Preferred options, in order:

1. EF Core migration bundle
2. dedicated migration executable/project
3. controlled `dotnet ef database update` command if necessary

Prefer migration bundles or a deploy-safe executable over installing SDK tooling in the runtime container.

Create scripts if useful:

```text
scripts/build-migrations.*
scripts/run-migrations.*
```

or equivalent cross-platform scripts.

### Important

Do not automatically run destructive migrations against any unknown or production database.

Document the production migration command for Render's pre-deploy phase.

Add guidance on expand/contract schema evolution.

---

# 12. Stage J — GitHub Actions Continuous Integration

Create:

```text
.github/workflows/ci.yml
```

## Triggers

Run at minimum on:

- pull requests targeting `master`
- pushes to `master`

Optionally include relevant development branches if the repository already uses them.

## .NET jobs

Perform:

```text
checkout
setup .NET
restore
build Release
test
```

Use dependency caching where appropriate.

## Angular jobs

Perform:

```text
checkout
setup Node
npm ci
lint (if configured)
test (if CI-capable)
production build
```

Do not invent lint/test commands that the project does not support; either implement the missing capability when reasonable or document it.

## Docker validation

Prefer adding a CI job that builds:

- API Docker image
- Worker Docker image

It does not need to push the images initially.

## CI result

Any failing required test/build must fail the workflow.

No deployment from `ci.yml` is required initially.

---

# 13. Stage K — Nightly Validation Workflow

Create:

```text
.github/workflows/nightly.yml
```

## Purpose

Nightly validation is not a nightly production deployment.

Use a nightly schedule in:

```text
America/Indiana/Indianapolis
```

Prefer a low-traffic time such as approximately 2:00 AM local time.

Also allow:

```text
workflow_dispatch
```

for manual execution.

## Tasks

Run a superset of CI validation where practical:

- clean .NET restore/build
- full .NET tests
- Angular clean install
- Angular production build
- Angular tests
- Docker image builds
- temporary PostgreSQL service/container
- apply migrations against empty database
- integration tests that can safely run in CI
- dependency/security audit commands that do not require paid services

Do not deploy production from the nightly workflow.

If some checks are currently impossible, leave clear TODOs and document them rather than creating fake passing checks.

---

# 14. Stage L — Render Blueprint

Create:

```text
render.yaml
```

Use Render Blueprint/IaC syntax appropriate at implementation time.

Describe the production topology where practical:

- Angular static site
- ASP.NET Core API web service
- .NET Worker background worker
- PostgreSQL database or documented database binding
- health check path
- build/start commands
- pre-deploy migration command if appropriate
- environment-variable declarations without secret values

Use environment-variable placeholders / secret declarations.

Do not commit real secrets.

## Important

If Render's Blueprint feature cannot safely or correctly provision a particular component under the intended account/plan, document the human provisioning step rather than forcing it.

---

# 15. Stage M — Deployment Documentation

Create:

```text
docs/deployment.md
```

This should be the human-facing deployment runbook.

Include:

1. prerequisites
2. repository readiness
3. Render account setup
4. Render PostgreSQL creation
5. API service creation
6. Worker service creation
7. Angular static-site creation
8. environment-variable configuration
9. migration configuration
10. health checks
11. deploy-after-CI settings
12. domain setup
13. DNS setup
14. email provider setup
15. OAuth provider setup
16. smoke testing
17. rollback instructions
18. backup verification
19. staging setup

Use placeholders such as:

```text
<ROOT_DOMAIN>
<API_DOMAIN>
<RENDER_DATABASE_URL>
<RESEND_API_KEY>
<GOOGLE_CLIENT_ID>
```

Never use invented credentials.

---

# 16. Stage N — Human Configuration Checklist

Create:

```text
docs/human-deployment-checklist.md
```

This file must clearly distinguish human-owned steps from agent-owned implementation.

Use checkboxes.

Required human steps should include at least:

## Accounts / billing

- [ ] Create or confirm GitHub repository/account
- [ ] Create Render account/workspace
- [ ] Select acceptable Render plan
- [ ] Create Cloudflare account
- [ ] Purchase/select production domain
- [ ] Create Resend account
- [ ] Create Google Cloud project
- [ ] Create Discord Developer application
- [ ] Create Meta/Facebook Developer application

## Domain

- [ ] Add domain to Cloudflare
- [ ] Configure DNS records
- [ ] Configure root/www frontend hostnames
- [ ] Configure API hostname
- [ ] Verify HTTPS works

## Database

- [ ] Create/approve production PostgreSQL resource
- [ ] Supply production connection string through Render secret/environment configuration
- [ ] Confirm backups/retention
- [ ] Approve first production migration

## Email

- [ ] Verify sending domain
- [ ] Add SPF record
- [ ] Add DKIM record(s)
- [ ] Add DMARC record
- [ ] Create Resend API key
- [ ] Add Resend API key to production environment
- [ ] Set From address
- [ ] Send test transactional email

## Google OAuth

- [ ] Create production OAuth client
- [ ] Configure authorized origin(s), if required
- [ ] Configure exact redirect URI
- [ ] Add Client ID secret/config value
- [ ] Add Client Secret
- [ ] Test production login

## Discord OAuth

- [ ] Create/configure application
- [ ] Configure redirect URI
- [ ] Add Client ID
- [ ] Add Client Secret
- [ ] Test production login

## Facebook OAuth

- [ ] Create/configure Meta application
- [ ] Enable Facebook Login as required
- [ ] Configure valid OAuth redirect URI/domain
- [ ] Add App ID
- [ ] Add App Secret
- [ ] Complete any required app-mode/review steps
- [ ] Test production login

## Deployment

- [ ] Connect Render to GitHub
- [ ] Approve service creation
- [ ] Enter required secrets
- [ ] Verify CI passes
- [ ] Perform initial deployment
- [ ] Verify health endpoint
- [ ] Verify Angular site
- [ ] Verify API access
- [ ] Verify worker starts
- [ ] Verify database connectivity
- [ ] Verify email
- [ ] Verify OAuth providers

---

# 17. Stage O — Production Smoke Test Script

Implement a safe automated smoke-test script.

Possible location:

```text
scripts/smoke-test.*
```

The script should accept configuration such as:

```text
FRONTEND_URL
API_URL
```

It should verify, at minimum:

- frontend returns success
- API health endpoint returns success
- expected API version/status endpoint if one exists

Do not automate login using real Google/Discord/Facebook credentials.

Create:

```text
.github/workflows/smoke-test.yml
```

Initially support:

```text
workflow_dispatch
```

If safe and appropriate, it may later run after production deployment.

Do not make the workflow responsible for deploying production unless explicitly requested later.

---

# 18. Stage P — Staging Readiness

Do not require staging to launch the first test deployment, but make the repository capable of supporting it.

Document placeholders for:

```text
staging.<domain>
api-staging.<domain>
```

Staging must use:

- separate PostgreSQL database
- separate secrets
- preferably separate OAuth clients/app configuration
- separate email configuration where appropriate

Never permit staging to silently fall back to the production database.

Add environment validation that reduces the chance of this configuration mistake.

---

# 19. Stage Q — Backup and Restore Documentation

Managed PostgreSQL backup configuration is human/provider owned, but repository documentation is agent-owned.

Create:

```text
docs/database-backup-restore.md
```

Document:

- Render-managed backup/PITR settings to verify
- how to perform a logical `pg_dump`
- how to restore into a non-production database
- how to validate a backup
- recommendation that backups be stored independently from the primary database provider eventually
- requirement to test restoration before the real campaign begins

Do not embed passwords in scripts.

Provide scripts using environment variables when helpful.

---

# 20. Stage R — Error Monitoring Readiness

Do not require Sentry or another paid/external monitoring service for the initial deployment.

Prepare the code so an error-monitoring provider can be added later without major restructuring.

If a monitoring package is already present, configure it through environment variables.

Otherwise:

- document recommended integration point
- ensure global exception handling and structured logging already provide useful diagnostics

Do not add Sentry purely to satisfy this document unless the project clearly benefits and the dependency is low-risk.

---

# 21. Things the Agent Should NOT Add Yet

Do not introduce these without a demonstrated requirement:

```text
Kubernetes
Terraform
RabbitMQ
Kafka
Redis
Elasticsearch
service mesh
Grafana
Prometheus
microservices
multiple API replicas
complex message queues
```

The initial architecture should remain approximately:

```text
Angular
   |
   v
ASP.NET Core API
   |
   v
PostgreSQL

.NET Worker
   |
   v
PostgreSQL
```

External services:

```text
Resend
Google OAuth
Discord OAuth
Facebook OAuth
Cloudflare DNS
```

---

# 22. Suggested Implementation Order

The agent should implement the work in the following order unless repository findings justify a change:

## Phase 1 — Production readiness

1. Repository audit
2. Environment-safe configuration
3. Dockerfiles
4. health endpoints
5. logging/correlation
6. email abstraction/Resend implementation
7. authentication configuration cleanup
8. Angular production configuration
9. migration tooling

### Completion gate

At the end of Phase 1:

- local development still works
- API Docker image builds
- Worker Docker image builds
- Angular production build succeeds
- tests pass
- no external production accounts are required

---

## Phase 2 — CI

1. `ci.yml`
2. Docker build validation
3. migration validation against temporary PostgreSQL
4. `nightly.yml`

### Completion gate

A fresh GitHub checkout can build/test the entire repository without relying on the developer workstation.

This is a major milestone.

---

## Phase 3 — Deployment definition

1. `render.yaml`
2. deployment runbook
3. human checklist
4. smoke-test script/workflow
5. staging documentation
6. database backup/restore documentation

### Completion gate

The repository contains everything needed for the human developer to begin provisioning external services.

---

## Phase 4 — Human-assisted initial cloud deployment

The agent should stop and request the human developer's external configuration as needed.

Likely human-provided values:

```text
ROOT_DOMAIN
production Render service/resource identifiers
PostgreSQL connection data
Resend API key
email From address
Google Client ID/Secret
Discord Client ID/Secret
Facebook App ID/Secret
```

The agent may help validate configuration files, DNS values, callback URLs, or deployment errors after the human supplies them.

---

## Phase 5 — Initial deployment validation

After the human provisions services:

1. run CI
2. deploy PostgreSQL/service definitions
3. run/approve migrations
4. deploy API
5. deploy Worker
6. deploy Angular
7. test `/health`
8. run smoke tests
9. test email
10. test Google login
11. test Discord login
12. test Facebook login
13. inspect logs
14. verify Worker execution
15. verify backup configuration

Do not declare production ready while any critical test fails.

---

# 23. Acceptance Criteria

The agent's implementation work is complete when all applicable items below are true.

## Repository

- [ ] All executable services identified
- [ ] Production Dockerfiles build successfully
- [ ] Local Docker/development workflow still works
- [ ] Environment configuration is documented
- [ ] No production secrets are committed
- [ ] `.env.example` exists
- [ ] deployment docs exist

## Backend

- [ ] API starts from Docker
- [ ] Worker starts from Docker
- [ ] API health endpoint exists
- [ ] PostgreSQL configuration comes from environment
- [ ] migrations can be applied without developer IDE intervention
- [ ] structured logs exist
- [ ] production proxy/HTTPS behavior supports OAuth redirects

## Frontend

- [ ] Angular production build succeeds
- [ ] API endpoint is environment configurable
- [ ] SPA routing deployment behavior is documented/configured
- [ ] no secrets exist in frontend configuration

## Email

- [ ] local email testing still works
- [ ] Resend implementation exists
- [ ] provider selection is configurable
- [ ] no real API key is committed

## Authentication

- [ ] Google configuration is externalized
- [ ] Discord configuration is externalized
- [ ] Facebook configuration is externalized
- [ ] callback URL requirements are documented
- [ ] secrets remain server-side

## CI

- [ ] PR/master CI builds .NET
- [ ] PR/master CI runs .NET tests
- [ ] PR/master CI builds Angular
- [ ] Docker images are validated
- [ ] nightly workflow exists
- [ ] nightly workflow validates migrations against temporary PostgreSQL where feasible
- [ ] nightly workflow does not deploy production

## Deployment

- [ ] `render.yaml` exists or a documented reason explains why a resource remains manual
- [ ] human deployment checklist exists
- [ ] smoke-test script exists
- [ ] staging model documented
- [ ] database backup/restore runbook exists

---

# 24. Agent Reporting Format

After each phase, provide the human developer with a concise report containing:

## Completed

List files created/modified and important functionality implemented.

## Validation

List commands/tests executed and whether they passed.

## Human action required

List only actions the agent cannot perform, such as:

- account creation
- billing selection
- purchasing a domain
- DNS ownership verification
- entering secrets
- OAuth console setup
- production database creation/approval
- deployment approval

## Risks / decisions

List anything that could materially affect production deployment.

## Next recommended phase

State the next stage that can be performed safely.

---

# 25. Grok 4.6-Specific Execution Guidance

This file is intended to be usable by Grok 4.6 or another repository-aware coding agent.

When executing this plan:

1. Read this entire document first.
2. Inspect the repository before writing code.
3. Do not blindly generate files based on the example project names.
4. Match the repository's existing naming and coding conventions.
5. Prefer editing existing abstractions rather than adding duplicate abstractions.
6. Run builds and tests frequently.
7. Keep changes grouped by phase so they are easy to review or revert.
8. Do not skip errors by weakening tests.
9. Do not mark TODO items complete unless the implementation actually exists.
10. Stop at external-service boundaries and clearly report the exact human action needed.
11. When a human-provided secret is required, provide the configuration key name but never request that the secret be committed to source control.
12. When unsure whether an external action is safe or billable, leave it for the human developer.
13. Preserve the ability to deploy in stages.
14. Prefer a working minimal production setup over speculative infrastructure.

---

# 26. Recommended First Agent Instruction

Use this as the first prompt after adding this document to the repository:

> Read `docs/AGENT_DEPLOYMENT_PLAN.md` completely, then inspect the repository. Implement **Phase 1 — Production readiness** only. Adapt the plan to the actual repository rather than assuming the sample project names are exact. Do not perform any external account, DNS, billing, OAuth-console, or production deployment actions. Keep local development working. Run all relevant builds and tests when finished. Then report completed work, validation results, human actions required, risks/decisions, and the recommended next phase. Do not proceed to Phase 2 until I instruct you to.

After reviewing Phase 1, the next prompt can be:

> Implement **Phase 2 — CI** from `docs/AGENT_DEPLOYMENT_PLAN.md`. Run/validate the workflows as far as possible from the repository environment. Do not deploy production. Report results using the required reporting format and stop before Phase 3.

This phased approach is recommended over asking the agent to perform the entire deployment conversion in one uninterrupted pass.

---

# 27. Final Deployment Principle

The target milestone before configuring external services is:

> A completely fresh checkout of the repository can build, test, create production-ready application artifacts/containers, validate database migrations, and document all required deployment settings without relying on software or secrets that exist only on the developer's computer.

Once that condition is true, external provisioning and production deployment can begin with much lower risk.
