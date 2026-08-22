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
- Commit, uncommit only while the action window is open, deadline auto-submit, and default Hold.
- Action validity and precedence, especially Battle overriding later actions.
- Move adjacency, spawn restrictions, split/rejoin with a play-log entry, backstab, pillage/repair, and retreat.
- Supply graph traversal, alliance inclusion, temporary supply, and split forces.
- Status transitions and faction exceptions, including configured force-status enable/clear
  triggers and catalog order when more than one trigger matches.
- Public/private objective visibility, completion, manager approval of private claims, and automatic private-objective scoring.
- Relic discovery, transfer, drop, choice resolution, destroy-and-replace, tie-breaking, and secrecy.
- Campaign-point components and graph objectives.
- Public-objective award/revoke facts and hidden item-objective standings secrecy.
- Battle-submission equivalence, single submission, disagreement, and GM resolution.
- Campaign-preset save copies map image and overlay; apply remaps overlay catalog identifiers by name.
  Saving the same name after trimming whitespace overwrites the previous preset.

## Backend integration tests

Use `WebApplicationFactory` and a disposable PostgreSQL instance. Do not substitute SQLite for
PostgreSQL behavior.

Cover:

- Identity registration/login and campaign-scoped permission policies.
- Player, player-GM, neutral GM, and administrator capabilities.
- Database constraints, migrations, optimistic concurrency, and transactions.
- Final commitment and deadline processing under concurrent requests.
- API response shapes that omit orders, hidden item objectives, relics, and audit data from unauthorized
  callers.
- GM inspection/correction audit events and transactional notification outbox.
- Idempotent deadline workers and retry behavior.
- File metadata and upload authorization.
- Public campaign-log chat on an upcoming campaign, including outsider rejection, unknown `@` mentions, and private-channel omission from unauthorized payloads.
- Public site chat on All Campaigns, including unknown `@` mentions, prohibited language, mutual blocks, isolation from campaign logs, administrator announcements with notifications, and rejection of seeded test accounts.
- Public profile campaign lists that include shared or publicly viewable campaigns and omit hidden private campaigns the viewer does not share.
- Home notification board empty and populated states, and administrator-only news edits.
- Manager add and kick of players (including private campaigns without the join password), staff
  faction assignment, and administrator impersonation of seeded test accounts.
- Administrator save-as-preset copies the map file and overlay graph; applying onto another campaign
  remaps overlay terrain identifiers onto that campaign's catalog.

## Angular tests

Use Angular's Vitest integration.

Cover components/services for:

- Order drafting from the map menu or force-panel checkmark, commit only when every required draft is saved, uncommit, and locked state.
- Countdown display without treating the browser clock as authoritative.
- Map territory selection, force markers, polygon editing including Close Territory enclose and
  shared-border versus overlapping-interior checks, move drop validity, keyboard alternatives,
  collapsible territory fields and list, overlay and connection visibility toggles, selected-territory
  dimming, black connection arrows without size or outline changes, spawn ownership copy, required-
  subfaction spawn labels, disabled no-fixed-spawn factions, save-status check and X, and metadata forms.
  The map editor does not show hover-placeholder copy above the map, and hovering or selecting a
  territory does not change that field panel's height. The expanded Territories list stays within the
  map column height, scrolls vertically, and scrolls the topmost selected territory into view.
  Administrators can save as a preset from the map editor; the save-name lookup includes The Hunt in
  Estalia. Map PNG downloads rasterize unselected overlay fills, spawn hatching, structure pins, and
  faction flags or logos, and omit adjacency arrows. Downloaded flags are twice the on-map marker
  size and structures are three times that size.
- Permission-based navigation without relying on it as backend security.
- Battle submissions, dispute state, notifications, objectives, relic visibility, and audits.
- Campaign log display, member chat including typable recipient autocomplete and public/private/game-log filters, live log refresh, chat send errors without the save success
  banner, `@` mention autocomplete limited to current members, clickable originator and mention names, and manager or administrator download of public chat and/or game-log facts as text or CSV.
- Public site chat on All Campaigns, including language filters, block toggles, administrator compose, and cookie-stored language preferences.
- Participants panel names, factions, and Manager/Player/Admin roles, including manager add/search/kick and staff faction assignment.
- Administrator test-users page and the impersonation banner with Return to admin.
- Public profile campaign list, scores placeholder, and Back to the previous in-app screen.
- Home notification board, including "No new notifications.", and paginated site news.
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
