---
applyTo: 'src/Campaign.Web/**/*.{ts,html,css,json}'
---

# Frontend Instructions

Read `/AGENTS.md`, `/docs/PRODUCT.md`, `/docs/SECURITY.md`,
`/docs/CODING-STANDARDS.md`, and `/docs/TESTING-STRATEGY.md`.

- Use standalone, zoneless Angular components and strict templates.
- Prefer signals for synchronous UI state and RxJS for asynchronous streams.
- Keep components thin and campaign rules on the server/domain side.
- Do not expose hidden data through route resolvers, source maps, logs, cached state, or API
  over-fetching.
- Use semantic HTML, keyboard support, visible focus, and accessible names.
- Do not introduce a component or state library without an explicit decision.
- Add or update Vitest tests with behavior changes.
