# Application Project Instructions

These instructions extend the repository `AGENTS.md`.

- Implement named use cases; do not place HTTP or EF-specific code here.
- Authorize campaign-scoped intent before loading or mutating sensitive state.
- Coordinate transactions, domain operations, audit facts, and outbox messages.
- Separate commands from read models where that reduces secret-data exposure.
- Define ports for persistence, clock, randomness, identity, storage, and notifications.
- Preserve actual/effective actor attribution for staff-on-behalf-of operations.
- Add unit tests for orchestration and integration tests for adapter behavior.
