# API Project Instructions

These instructions extend the repository `AGENTS.md`.

- Endpoints validate transport shape, invoke Application use cases, and map results.
- Do not implement campaign resolution or permission logic in controllers/endpoints.
- Use named authorization policies and campaign membership checks.
- Publish OpenAPI and stable machine-readable errors.
- Avoid over-fetching: public, private, and staff views use purpose-specific contracts.
- Apply rate limits to authentication, uploads, and high-impact staff commands.
- Add `WebApplicationFactory` integration tests for endpoints, permissions, serialization,
  database behavior, concurrency, and secrecy.
