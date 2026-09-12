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
- The `anyComponentStyle` budget in `angular.json` is 8 kB warning and 12 kB error, raised from the
  Angular scaffold defaults of 4 kB and 8 kB. Component styles are inlined into their route's lazy
  chunk and served with immutable cache headers, so the largest of them costs about 2.5 kB gzipped
  once per visitor; the budget exists to flag a stylesheet that has outgrown its component, not to
  cap transfer size. The map view and campaign detail pages are legitimately large interactive
  components and sat within a few hundred bytes of the old error threshold, where an unrelated
  styling tweak failed the build. Treat a warning as a prompt to check for a component worth
  extracting, and delete rules whose classes appear in no template rather than carrying them.
- Form submissions show the shared saving overlay until the request settles, including success,
  failure, and exceptions. Pages that remain after a successful save show a green banner:
  "Successfully saved changes."
- Generated OpenAPI client output is regenerated, never manually edited.
