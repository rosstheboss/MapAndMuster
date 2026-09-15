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
- Ending a campaign against a stale client revision and retrying when another write moves the revision.
  Deleting a completed campaign is limited to managers and administrators; open campaigns are rejected.
- Commit, uncommit only while the action window is open, deadline auto-submit, and default Hold.
  Retreat commit and uncommit while the applying battle window is open, last-commit early battle
  close when every result, surrender, and required retreat is committed even if the Battle-phase
  early-close checkbox is off, idle battle windows staying open for a ringer when that checkbox is
  off, auto-commit of a sole legal retreat destination including spawn, deadline use of an
  uncommitted retreat draft, and default spawn when no retreat exists.
  Players whose forces are all in battle (or otherwise owe no order) appear as committed on the
  action roster; Actions names the locked battle's territory and opponents.
- Action validity and precedence, especially Battle overriding later actions.
- Move adjacency, spawn restrictions (no landing on spawn; pass through own spawn only), split/rejoin with a play-log entry, backstab, pillage/repair, and retreat
  (Neutral reachability at movement speed, blocked by enemies or battles on the path, allied
  occupation of the destination, two enemy landings both sent to spawn, pass-through of another
  enemy's landing hex).
  `UndergroundNetwork` uses the same Town/City pick for initial placement and a missing or
  ineligible retreat (or collision fallback) that would otherwise send the force to spawn.
- Supply graph traversal, alliance inclusion, temporary supply, and split forces.
- Status transitions and faction exceptions, including configured force-status enable/clear
  condition lists, location filters (any, type, or tag), consecutive occurrence counts, unique
  priorities when more than one trigger matches, save-time duplicate/redundancy collapse, and
  optional cancel-out pairs. Named Diseased catalog conditions plus remaining contagion/rejoin/
  plague/immunity engine behavior. Catalog tags (uniqueness, scoped assignment, subfaction union,
  Water replacing the former water-feature flag). Private and public objective tag filters.
- Public/private objective visibility, completion, manager approval of private claims, automatic
  private-objective scoring and live `(current/required)` progress for authorized holders, and launch
  assignment (unique draws per holder-kind pool, then reshuffled duplicates until every holder in a
  non-empty pool has an independent assignment; required subfactions are separate faction holders).
  Player-held automatic progress counts only that player's credited holdings. Private-objective exclude lists skip named factions
  and ally groups. Rival objectives seed unique enemies, reveal on battle or surrender, and replenish
  closest unused rivals on later non-final action rounds.
- Relic discovery, transfer, drop, choice resolution, destroy-and-replace, tie-breaking, and secrecy.
- Campaign-point components and graph objectives. Map holdings are attributed to individual
  players on a shared faction, not copied from a faction-wide total onto every co-faction player.
- Public-objective award/revoke facts and hidden item-objective standings secrecy.
- Battle-submission equivalence, single submission, disagreement, and GM resolution.
- Campaign-preset save copies map image, overlay, and catalog files; apply remaps overlay catalog
  identifiers by name and copies uploaded logos onto matching catalog names.
  Saving the same name after trimming whitespace overwrites the previous preset.
  Administrator preset-package download/upload copies catalog, overlay JSON, map image, and catalog
  files including logos; non-administrators are rejected. Re-uploading the same collapsed name
  overwrites one named preset and reuses identical map and catalog file bytes.

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
- Home notification board empty and populated states, dismiss and dismiss-all, five notices per
  page, two news articles per page, and administrator-only news edits.
  Home's Needs your attention list is built from `GET /api/campaigns` (in-progress round,
  countdown, commit, remaining setup) and sits above Notifications and News.
- Manager add and kick of players (including private campaigns without the join password), promoting a
  player to campaign manager, adding a manager-only member, staff faction assignment, ending a
  campaign while keeping its final state (including a stale client revision after play has advanced),
  deleting a completed campaign (and rejecting delete while the campaign is still open),
  GET play returning no content before the start instant,
  and administrator impersonation of seeded test accounts.
- Administrator save-as-preset copies the map file, overlay graph, and uploaded catalog logos;
  applying onto another campaign remaps overlay terrain identifiers onto that campaign's catalog
  and copies matching logos.
- Administrator download/upload of a `.mapandmuster-preset` ZIP round-trips map image, overlay
  graph, and catalog logos; non-administrators receive 403. Re-import of the same collapsed name
  keeps a single named preset and reuses unchanged file bytes.

## Angular tests

Use Angular's Vitest integration.

Cover components/services for:

- Order drafting from the map menu or force-panel **Save draft**, including a chosen item-objective teleport destination, commit only when every required draft is saved, uncommit only while the action window is open, and a confirming last-commit dialog when every other player is already committed. Confirmation alertdialogs trap Tab, confirm on Enter, and cancel on Escape. A two-or-more-territory Move or Split always picks each via on the map, including a unique legal route. The Actions Commitments player list starts collapsed and keeps the "X of Y players committed. Waiting on …" summary under the Commitments heading. Expanding it shows players in alphabetical username order in up to three columns, filling left to right then top to bottom; each player lists username with a profile link and Drafting or Committed, then faction and subfaction, then that player's force locations as map links joined with "and".
- Campaign-page status bar (round/phase, throttled countdown live region, viewer commit chip, compact commitment count, Go to your orders). While a campaign is running, Actions, Chat, and Standings are open by default; other sections stay collapsed and the last set is stored in a per-campaign cookie. Staff tools are under collapsed Manage campaign. Battle, campaign, phase, and force-status enums use display labels. Summary ends with one Conduits of Power notice per adjacent force (`A hidden Relic is nearby the force at {territory}.`, bold and faction-colored glow) and each battle reminder renders once. The campaign log summary shows unread mention and private counts from `GET /log` without marking the log read on load. Log timestamps sit after the entry text (relative when under 24 hours). Scheduled campaign pages list faction and subfaction special rules in Factions and under the Summary faction selector from the campaign catalog, including when `GET /play` has not started or returns an empty rule list. Campaign, Edit campaign, and map editor pages end with Back to top.
- Create/edit campaign starts with Campaign details, Schedule, Factions, Terrain types, and Campaign map expanded; optional sections start collapsed. The sticky toolbar shows remaining required sections, nested mission groups have unique names, and Edit map is hidden after a campaign starts. Force-status cancel-out is a dropdown that adds named statuses to a removable list. Private-objective exclude lists add factions and ally groups the same way. Secret rival objectives default on with 5 campaign points. The campaign page lists a secret rival with the award as `(5 CP)` and the viewer's private objectives with `(X CP)`. Hovering a standings points cell lists that column's sources and that cell's total. Enable and clear each have a consecutive-occurrence integer from 1 to 10, a location filter (any, type, or tag), and Add that does not hide a trigger already in the list. Each catalog section has a tag subpanel whose name field adds on Enter or comma without saving, and item chip comboboxes that suggest unassigned defined tags.
- Countdown display without treating the browser clock as authoritative.
- Map territory selection, force markers, polygon editing including Close Territory enclose and
  shared-border versus overlapping-interior checks, move drop validity, keyboard alternatives,
  collapsible territory fields and list, overlay and connection visibility toggles, selected-territory
  dimming, black connection arrows without size or outline changes, secret one-way black order-route
  arrows per hop for the acting player's drafted or committed Move, Split, or Retreat (and staff in
  Debug) without hit targets, spawn ownership copy, required-
  subfaction spawn labels, disabled no-fixed-spawn factions, save-status check and X, and metadata forms.
  Map pinch-zoom and two-finger pan, full-screen toggle (M), map-image loading ellipsis, and force
  markers staying inside their territory are covered in map-view tests. Conduits of Power force pins
  adjacent to a still-hidden relic glow white. Own-force pins show a
  green-and-white check emblem half the pin's size, centered on the circular pin's top-right
  edge so half of it overlaps the pin, when that force has a saved draft or committed order, with hover and
  accessible text naming the action and draft versus committed. Cycle forces sits next to Full
  screen, selects that force's territory, and zooms to the force, its territory, and reachable Move
  destinations, or Fit when that frame cannot be computed. Cycle forces uses Y. The same Commit or
  Uncommit control as Actions sits between Cycle forces and Show names, including last-commit
  confirmation and a disabled Commit when drafts are incomplete. C commits when that control is
  enabled, or uncommits when Uncommit is shown. Force dots use faction or required-subfaction colors, not
  logos; ownership flags and logos stay with the territory owner. Force dots stay off flags and
  structure logos, shrinking no more than 50%. Required-subfaction claims keep that subfaction's color
  flag or uploaded logo instead of the parent faction mark. Subfaction colors, color flags, and uploaded logos follow
  the same uniqueness and tint rules as faction flags.
  Full-screen map mode keeps the image inside the viewport: a fitted map recenters after the panel
  resizes, and a zoomed map clamps pan so it cannot sit off-screen.
  Map zoom on the campaign page opens at Fit for scheduled and completed campaigns, or framed on
  the viewer's first owned force during an in-progress campaign. The map editor restores the last
  Fit-or-percent choice from `localStorage` per campaign.
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
  an open battle, and a retreating force after a loss or surrender. Clicking your force opens
  Surrender while engaged, or the usual action menu otherwise. Retreat and surrender commit and
  uncommit use the map toolbar.
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
  banner, `@` mention autocomplete limited to current members, clickable originator and mention names, bold campaign start/round/phase/end entries, resolved actions of a closed phase appearing before the next round/phase heading, and manager or administrator download of public chat and/or game-log facts as text or CSV.
- Battles panel collapse, a top-of-panel list of who still needs to commit a result or retreat,
  required army-points and supply-costing fields for the inputting player,
  Awaiting Retreat Order while a committed retreat is still owed, and retreat and surrender
  commit/uncommit on the map toolbar matching Action-phase commit.
- Public site chat on All Campaigns, including language filters, block toggles, administrator compose with bold announcement text, and cookie-stored language preferences.
- Participants panel names, factions, and Manager/Player/Admin roles, including manager add/search/kick, staff faction assignment, and a May be kicked badge that opens the delinquency log entry.
- Administrator test-users page (filter, Currently testing chip) and the impersonation banner with Return to admin.
- Public profile campaign list, scores placeholder, and Back to the previous in-app screen.
- Home notification board, including "No new notifications.", dismiss and dismiss all, five
  notices per page, Needs your attention from the campaign list, empty join/create actions, and
  two news articles per page.
- Campaign cards show status, round, countdown, player count, role, remaining setup, commit
  state, and Open while collapsed. Duplicate campaign and deleting a completed campaign both
  require a confirmation dialog. Empty Your campaigns offers Join campaign. All campaigns
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
