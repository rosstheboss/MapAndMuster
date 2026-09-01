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
- Public/private objective visibility, completion, manager approval of private claims, automatic
  private-objective scoring, and launch assignment (unique draws per holder-kind pool, then
  reshuffled duplicates until every holder in a non-empty pool has an independent assignment).
- Relic discovery, transfer, drop, choice resolution, destroy-and-replace, tie-breaking, and secrecy.
- Campaign-point components and graph objectives.
- Public-objective award/revoke facts and hidden item-objective standings secrecy.
- Battle-submission equivalence, single submission, disagreement, and GM resolution.
- Campaign-preset save copies map image, overlay, and catalog files; apply remaps overlay catalog
  identifiers by name and copies uploaded logos onto matching catalog names.
  Saving the same name after trimming whitespace overwrites the previous preset.
  Administrator preset-package download/upload copies catalog, overlay JSON, map image, and catalog
  files including logos; non-administrators are rejected.

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
  Home's Needs your attention list is built from `GET /api/campaigns` (in-progress round,
  countdown, commit, remaining setup) and sits above Notifications and News.
- Manager add and kick of players (including private campaigns without the join password), promoting a
  player to campaign manager, adding a manager-only member, staff faction assignment, ending a
  campaign while keeping its final state, and administrator impersonation of seeded test accounts.
- Administrator save-as-preset copies the map file, overlay graph, and uploaded catalog logos;
  applying onto another campaign remaps overlay terrain identifiers onto that campaign's catalog
  and copies matching logos.
- Administrator download/upload of a `.mapandmuster-preset` ZIP round-trips map image, overlay
  graph, and catalog logos; non-administrators receive 403.

## Angular tests

Use Angular's Vitest integration.

Cover components/services for:

- Order drafting from the map menu or force-panel **Save draft**, commit only when every required draft is saved, uncommit only while the action window is open, and a confirming last-commit dialog when every other player is already committed.
- Campaign-page status bar (round/phase, throttled countdown live region, viewer commit chip, compact commitment count, Go to your orders). While a campaign is running, Actions, Chat, and Standings are open by default; other sections stay collapsed and the last set is stored in a per-campaign cookie. Staff tools are under collapsed Manage campaign. Battle, campaign, phase, and force-status enums use display labels. A hidden-relic notice and each battle reminder render once. The campaign log summary shows unread mention and private counts from `GET /log` without marking the log read on load. Log timestamps sit after the entry text (relative when under 24 hours).
- Create/edit campaign starts with Campaign details, Schedule, Factions, Terrain types, and Campaign map expanded; optional sections start collapsed. The sticky toolbar shows remaining required sections, nested mission groups have unique names, and Edit map is hidden after a campaign starts.
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
  Map zoom defaults to Fit and is restored from `localStorage` per campaign.
  Selecting a territory or group from outside the map pans to center it without leaving image bounds,
  and zooms out only when the current scale cannot encapsulate the selection, never past Fit.
  Territory hit polygons are named buttons; keyboard focus and Enter/Space select a territory, and a
  collapsible display-number-ordered directory is the accessible alternative on the campaign map
  (hidden in the map editor, which keeps its own legend and list). Campaign territory details sit under the map
  in the left column, not under the directory. That details panel keeps a reserved height whether
  empty or populated and scrolls overflow so hovering or selecting a territory does not grow the
  campaign page or shrink the full-screen map. The campaign map Territories list stays within the
  map column height, scrolls vertically, and shrinks when Map legend is expanded so the Map panel
  does not grow. The map editor uses the same collapsible Map legend above its Territories list in the
  right column; expanding the legend shrinks the list so the editor layout does not grow. Show-names
  labels stay screen-sized while zoomed and
  use theme surface/text colors. Named territories keep their full name at any size; unnamed display
  numbers hide when they would not fit. N toggles Show names. Hovering a map territory or a
  Territories row shows name, owner or Neutral, structure (with pillaged state), terrain, forces,
  an open battle, and a retreating force after a loss or surrender.
  Playwright axe includes map polygons; a Playwright test
  tabs to a territory, presses Enter, and asserts the details panel updates without changing height.
  The map editor does not show hover-placeholder copy, and hovering or selecting a
  territory does not change that field panel's height. The Territory editor sits below the map.
  The Territories list starts expanded, stays
  within the map column height, scrolls vertically, and scrolls the topmost selected territory into
  view. Mode tools are grouped separately from Connections, Colors, and File commands, with Select
  first and selected by default, and the
  active mode does not use the primary Save Map color. Campaign and map-editor Territories rows
  show owner mark, optional structure, terrain type, then name. Edit map is hidden once a campaign is no
  longer Scheduled; opening the editor anyway returns to the campaign page with a notice.
  Administrators can save as a preset from the map editor; the save-name lookup includes The Hunt in
  Estalia. Edit campaign exposes administrator Download Preset and Upload Preset for a portable
  package of catalog, overlay, map image, and uploaded logos. Map PNG downloads rasterize unselected overlay fills, spawn hatching, structure pins, and
  faction flags or logos (including faction-color tints when enabled), and omit adjacency arrows. Downloaded flags are twice the on-map marker
  size and structures are three times that size. Uploaded overlay SVG remaps terrain, structures,
  owners, and spawns onto the current campaign catalog by name when identifiers differ.
- Permission-based navigation without relying on it as backend security. Below 45 rem the primary
  nav collapses behind a Menu button; Home and the theme toggle stay visible. Nav labels use
  sentence case. The theme toggle names the action, not the current mode.
- Registration and profile field rows, a Choose image file picker, visible fieldsets, and sticky
  Save on profile.
- Password fields include a show/hide toggle that restores `type=password`.
- Battle submissions, dispute state, notifications, objectives, relic visibility, and audits.
- Campaign log display, member chat including typable recipient autocomplete and public/private/game-log/delinquency filters, live log refresh, chat send errors without the save success
  banner, `@` mention autocomplete limited to current members, clickable originator and mention names, and manager or administrator download of public chat and/or game-log facts as text or CSV.
- Public site chat on All Campaigns, including language filters, block toggles, administrator compose, and cookie-stored language preferences.
- Participants panel names, factions, and Manager/Player/Admin roles, including manager add/search/kick, staff faction assignment, and a May be kicked badge that opens the delinquency log entry.
- Administrator test-users page (filter, Currently testing chip) and the impersonation banner with Return to admin.
- Public profile campaign list, scores placeholder, and Back to the previous in-app screen.
- Home notification board, including "No new notifications.", Needs your attention from the
  campaign list, empty join/create actions, and paginated site news.
- Campaign cards show status, round, countdown, player count, role, remaining setup, commit
  state, and Open while collapsed. Empty Your campaigns offers Join campaign. All campaigns
  shows collapsed Site chat above the campaign list.
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
