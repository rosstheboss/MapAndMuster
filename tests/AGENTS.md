# Test Project Instructions

These instructions extend the repository `AGENTS.md` and `docs/TESTING-STRATEGY.md`.

- Tests document behavior; do not mirror implementation details unnecessarily.
- Use fake time and deterministic randomness; no real deadline sleeps.
- Persistence integration tests use a disposable PostgreSQL database.
- Create explicit player, player-GM, neutral GM, and administrator identities.
- Assert that secrets are absent from unauthorized API payloads and logs.
- Exercise concurrency for final commitment, phase processing, and GM corrections.
- Keep fixtures generic and fictional; never copy proprietary game content.
- Never weaken, skip, or delete a test solely to accept generated code.
