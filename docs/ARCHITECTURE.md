# Architecture

## Style

Use a modular monolith with two deployable applications: ASP.NET Core API and Angular web.
Do not introduce microservices or full event sourcing without an architecture decision record.

## Project responsibilities

### Campaign.Domain

Pure domain model and rules: campaigns, memberships, rounds, phases, action windows, orders,
forces, territories, structures, alliances, battles, submissions, retreats, missions, supply,
statuses, objectives, relics, scoring, notifications requested by domain events, and audit facts.

It may depend only on the .NET base class libraries. It must not reference EF Core, ASP.NET
Core, Identity, email, storage, or logging implementations.

### Campaign.Application

Use cases, commands/queries, permission checks, transaction boundaries, validation that spans
aggregates, domain-event handling, DTO mapping, and ports such as clock, email outbox,
identity, file storage, and persistence abstractions.

It depends only on Domain.

### Campaign.Infrastructure

EF Core/PostgreSQL, Identity stores, external-login adapters, email sender, transactional
outbox worker, uploaded-asset storage, implementation clock, and other adapters.

It depends on Application and Domain.

### Campaign.Api

HTTP endpoints, request validation, authentication, permission policies, OpenAPI, middleware,
rate limiting, health checks, dependency injection, and process startup.

It depends on Application and Infrastructure. Endpoints do not contain domain logic.

### Campaign.Web

Angular views and client-side interaction. Components collect intent and display server-provided
state. The backend remains authoritative for permissions, deadlines, secrets, calculations, and
resolution.

## Domain modules

- Identity and Membership
- Campaign Setup
- Map and Territory Graph
- Rounds and Action Windows
- Orders and Resolution
- Battles and Missions
- Supply and Army Composition
- Objectives and Scoring
- Relics and Hidden Information
- Notifications and Audit

Modules may initially share a database and process. Keep public module interactions explicit.

## Persistence

- PostgreSQL is authoritative.
- Use EF Core migrations committed to source control.
- Use optimistic concurrency tokens and a campaign revision for state-changing commands.
- Store timestamps as UTC instants.
- Store current state in normal relational tables and immutable history in audit/revision tables.
- GeoJSON or normalized polygon points may be stored as JSONB; PostGIS is not initially
  required.
- Never overwrite original orders or battle submissions when staff correct outcomes.
- Factions are stored relationally with a unique color and a flag that, when enabled, requires
  players who choose that faction to pick a subfaction.

## Map

- A sanitized raster image is the background.
- Territories use normalized polygon coordinates stored with the campaign overlay graph as JSONB.
- Terrain types, structures, and nested missions are stored with the campaign as JSONB catalogs.
- Structure logos and mission documents are stored outside web root; file keys are not returned to
  clients.
- Adjacency is explicit and validated; geometry may suggest but not silently establish it.
- Generate Connections may propose edges from shared borders; user-created edges persist across
  regeneration.
- The client renders an SVG overlay on the rectangular map image. Leaflet is not required for this
  overlay editor.
- Arbitrary active SVG upload is out of scope. Import only a validated application schema.

## API contracts

- Publish OpenAPI from the API.
- Generate the TypeScript client; do not hand-edit generated output.
- Separate public, participant-private, and staff response shapes to prevent over-fetching.
- Use stable identifiers and machine-readable error codes.
- Mutating requests that may be retried use idempotency or concurrency protection as needed.

## Background work

Database deadlines are authoritative. A worker may prompt transition processing, but every
transition command must be safe to repeat and must re-check database state.

Email uses an outbox written in the same transaction as the event that requested it. Delivery
failure does not roll back campaign state and remains visible for retry/operations.

## Authentication and authorization

- ASP.NET Core Identity supplies local accounts and optional Google, Facebook, and Discord
  login when credentials are configured. See `docs/adr/0002-external-login-providers.md`.
- Registration may be open, but joining a campaign requires authorization/invitation.
- Permission policies are derived from system role plus campaign membership roles.
- Staff acting for another party records actual and effective actor identities.
