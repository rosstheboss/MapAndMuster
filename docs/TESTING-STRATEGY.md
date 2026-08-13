# Testing Strategy

## Principles

Tests protect observable campaign behavior, authorization, secrecy, concurrency, and history.
Coverage percentage is initially informative rather than a release gate. Do not substitute high
line coverage for meaningful state-transition and boundary testing.

## Backend unit tests

Use xUnit for pure Domain and Application tests. Test each policy with table-driven cases where
useful.

Required early suites:

- Required-participant calculation and final-commit race behavior.
- Commit, uncommit, deadline auto-submit, and default Hold.
- Action validity and precedence, especially Battle overriding later actions.
- Move adjacency, spawn restrictions, split/rejoin, backstab, pillage/repair, and retreat.
- Supply graph traversal, alliance inclusion, temporary supply, and split forces.
- Status transitions and faction exceptions.
- Public/private objective visibility and completion.
- Relic discovery, transfer, drop, tie-breaking, and secrecy.
- Campaign-point components and graph objectives.
- Battle-submission equivalence, single submission, disagreement, and GM resolution.

## Backend integration tests

Use `WebApplicationFactory` and a disposable PostgreSQL instance. Do not substitute SQLite for
PostgreSQL behavior.

Cover:

- Identity registration/login and campaign-scoped permission policies.
- Player, player-GM, neutral GM, and administrator capabilities.
- Database constraints, migrations, optimistic concurrency, and transactions.
- Final commitment and deadline processing under concurrent requests.
- API response shapes that omit orders, objectives, relics, and audit data from unauthorized
  callers.
- GM inspection/correction audit events and transactional notification outbox.
- Idempotent deadline workers and retry behavior.
- File metadata and upload authorization.

## Angular tests

Use Angular's Vitest integration.

Cover components/services for:

- Order drafting, validation messages, commit/uncommit, and locked state.
- Countdown display without treating the browser clock as authoritative.
- Map territory selection, polygon editing, keyboard alternatives, and metadata forms.
- Permission-based navigation without relying on it as backend security.
- Battle submissions, dispute state, notifications, objectives, relic visibility, and audits.
- API error and concurrency-conflict recovery.

## End-to-end tests

Use Playwright with distinct browser contexts/accounts.

Critical journeys:

1. Register, verify, join a campaign, and receive a force.
2. Two players draft/commit; one uncommits before the final commitment.
3. Final required commitment closes and reveals exactly once.
4. Deadline submits a saved draft and creates Hold for a missing order.
5. Action 1 creates a battle and forces Action 2 to Battle.
6. Two participants submit matching, single, and conflicting battle results.
7. GM inspects an order; the player receives in-app and email/outbox evidence.
8. GM corrects prior state; affected current orders reopen and all affected players are notified.
9. Unauthorized users cannot obtain hidden relic/objective/order data through direct API calls.
10. Multiple GMs attempt conflicting interventions; one receives a safe concurrency response.

## Time and randomness

- Inject a clock and advance fake time; never use real sleeps for campaign deadlines.
- Inject seeded/random-choice abstractions and record random outcomes for replay/audit.
- Tests assert both outcome and explanation/audit facts for automatic resolution.

## Test-change policy

- Behavior change and tests land together.
- Bug fix includes a regression test where practical.
- Skipped/flaky tests require an owner, reason, and tracked remediation.
- Never weaken an assertion or broaden authorization to make a test pass.
