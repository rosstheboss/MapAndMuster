# Angular Web Project Instructions

These instructions extend the repository `AGENTS.md`.

- Angular 22, standalone, zoneless, strict TypeScript/templates, CSS, and 120-character width.
- Use signals for synchronous UI state and RxJS for asynchronous streams.
- Do not duplicate server domain rules; client validation improves UX but is not authority.
- Never assume route guards or hidden controls provide authorization.
- Do not request or cache secret fields that the current view does not require.
- Build accessible keyboard alternatives for map-only interactions.
- Do not add Angular Material or another component/state library without a recorded decision.
- Update Vitest tests with every behavioral UI/service change.
- Generated OpenAPI client output is regenerated, never manually edited.
