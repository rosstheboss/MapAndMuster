# Domain Project Instructions

These instructions extend the repository `AGENTS.md`.

- Keep this project free of EF Core, ASP.NET Core, Identity, serialization, file, email, and UI
  dependencies.
- Express campaign invariants in aggregates, value objects, policies, and domain services.
- Use injected abstractions for time and randomness when rules depend on them.
- Return explicit domain outcomes for expected invalid commands.
- Every rule or state-transition change requires focused unit tests.
- Resolution results must be deterministic from inputs or record every random choice.
- Do not implement items from `docs/DECISIONS-NEEDED.md` without a recorded decision.
