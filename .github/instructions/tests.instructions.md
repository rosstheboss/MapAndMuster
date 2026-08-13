---
applyTo: 'tests/**/*,src/Campaign.Web/**/*.spec.ts'
---

# Test Instructions

Read `/AGENTS.md` and `/docs/TESTING-STRATEGY.md`.

- Test observable behavior, permissions, state transitions, and invariants.
- Use a real disposable PostgreSQL database for persistence integration tests.
- Control time with an injected fake clock; never wait for real deadlines.
- Create distinct identities for player, player-GM, neutral GM, and administrator scenarios.
- Assert that unauthorized responses omit secrets, not merely that the UI hides them.
- Keep tests deterministic and independent. Document unavoidable retries or timing behavior.
