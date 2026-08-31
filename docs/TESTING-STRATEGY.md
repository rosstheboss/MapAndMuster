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
  Administrator preset-package download/upload copies catalog, overlay JSON, map image, and catalog
  files; non-administrators are rejected.

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
- Manager add and kick of players (including private campaigns without the join password), promoting a
  player to campaign manager, adding a manager-only member, staff faction assignment, ending a
  campaign while keeping its final state, and administrator impersonation of seeded test accounts.
- Administrator save-as-preset copies the map file and overlay graph; applying onto another campaign
  remaps overlay terrain identifiers onto that campaign's catalog.
- Administrator download/upload of a `.mapandmuster-preset` ZIP round-trips map image and overlay
  graph; non-administrators receive 403.

## Angular tests

Use Angular's Vitest integration.

Cover components/services for:

- Order drafting from the map menu or force-panel **Save draft**, commit only when every required draft is saved, uncommit only while the action window is open, and a confirming last-commit dialog when every other player is already committed.
- Campaign-page status bar (round/phase, throttled countdown live region, viewer commit chip, compact commitment count, Go to your orders). While a campaign is running, Actions, Chat, and Standings are open by default; other sections stay collapsed and the last set is stored in a per-campaign cookie. Staff tools are under collapsed Manage campaign. Battle, campaign, phase, and force-status enums use display labels. A hidden-relic notice and each battle reminder render once. The campaign log summary shows unread mention and private counts from `GET /log` without marking the log read on load.
- Countdown display without treating the browser clock as authoritative.
- Map territory selection, force markers, polygon editing including Close Territory enclose and
  shared-border versus overlapping-interior checks, move drop validity, keyboard alternatives,
  collapsible territory fields and list, overlay and connection visibility toggles, selected-territory
  dimming, black connection arrows without size or outline changes, spawn ownership copy, required-
  subfaction spawn labels, disabled no-fixed-spawn factions, save-status check and X, and metadata forms.
  Map pinch-zoom and two-finger pan, full-screen toggle (M), map-image loading ellipsis, and force
  markers staying inside their territory are covered in map-view tests. Force dots stay off flags and
  structure logos, shrinking no more than 50%. Subfaction colors, color flags, and uploaded logos follow
  the same uniqueness and tint rules as faction flags.
  Full-screen map mode keeps the image inside the viewport: a fitted map recenters after the panel
  resizes, and a zoomed map clamps pan so it cannot sit off-screen.
  Territory hit polygons are named buttons; keyboard focus and Enter/Space select a territory, and a
  collapsible display-number-ordered directory is the accessible alternative on the campaign map
  (hidden in the map editor, which keeps its own list). Campaign territory details sit under the map
  in the left column, not under the directory. Show-names labels stay screen-sized while zoomed and
  use theme surface/text colors. Playwright axe includes map polygons; a Playwright test
  tabs to a territory, presses Enter, and asserts the details panel updates.
  The map editor does not show hover-placeholder copy above the map, and hovering or selecting a
  territory does not change that field panel's height. The expanded Territories list stays within the
  map column height, scrolls vertically, and scrolls the topmost selected territory into view.
  Administrators can save as a preset from the map editor; the save-name lookup includes The Hunt in
  Estalia. Edit campaign exposes administrator Download Preset and Upload Preset for a portable
  package of catalog, overlay, and map image.   Map PNG downloads rasterize unselected overlay fills, spawn hatching, structure pins, and
  faction flags or logos (including faction-color tints when enabled), and omit adjacency arrows. Downloaded flags are twice the on-map marker
  size and structures are three times that size. Uploaded overlay SVG remaps terrain, structures,
  owners, and spawns onto the current campaign catalog by name when identifiers differ.
- Permission-based navigation without relying on it as backend security.
- Password fields include a show/hide toggle that restores `type=password`.
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
