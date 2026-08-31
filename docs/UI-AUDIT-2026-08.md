# UI Audit — August 2026

A usability, accessibility, and visual-design audit of `MapAndMuster.Web` for the three
audiences in `PRODUCT.md`: administrators, campaign managers (GMs), and players.

This document is written to be implemented incrementally. Every recommendation is scoped so
that it changes presentation, layout, labelling, or focus behavior only. None of them require a
change to campaign rules, API contracts, authorization, or persisted state. Where a
recommendation touches a template that also carries logic, the logic is left alone.

- Status: in progress. Step 1 (`UI-C1`, `UI-H3`, `UI-H4`, `UI-M5`) is implemented. Step 2
  (`UI-C3`, `UI-C4`, `UI-M4`) is implemented (2026-08-30). Custom dialogs and confirm buttons.
  Campaign list items expose remaining-setup and commitment fields, and campaign-log last-read
  is persisted on the server (2026-08-30). Card badges, Home dashboard UI, and chat unread
  indicators still follow in later steps. Step 3 (`UI-C5`, `UI-H9`, `UI-H12`, Playwright axe)
  is implemented (2026-08-30). Map polygons (`UI-C2`) remain excluded from axe until step 4.
- Ranking: Critical, High, Medium, Polish.
- Each item has a stable identifier (`UI-C1`, `UI-H4`, …) so work can be tracked and split.

## How this audit was produced

1. Read `AGENTS.md`, `PRODUCT.md`, `ARCHITECTURE.md`, `SECURITY.md`, `SETUP.md`, and the
   nested `AGENTS.md` files, then `DOMAIN.md` and `CAMPAIGN-RULES-MATRIX.md` for the documented
   campaign rules the interface has to express. Two findings (`UI-H13`, `UI-M14`) come from
   comparing a documented rule against what the interface actually says.
2. Ran the application locally against a disposable `mapandmuster_uiaudit` database and a
   separate asset root. The existing "The Hunt for Estalia" campaign and its database were not
   read, copied, or modified.
3. Captured every signed-out and signed-in page from the live backend as an administrator.
4. Rendered the campaign play surfaces, the map, and the map editor against a mocked backend
   shaped by `PlayContracts.cs` and `CampaignContracts.cs`, using generic fictional content, so
   that a realistic mid-campaign state (four factions, twelve territories, drafts, commitments,
   an unresolved battle, standings, objectives) could be inspected without creating data.
5. Ran axe-core (WCAG 2.0/2.1 A and AA plus best practice) on every captured page.
6. Measured structure and density in the DOM: document heights, focusable counts, heading
   order, table semantics, computed contrast ratios, and dialog focus behavior.

### Evidence caveat

The play surfaces were rendered against mocked responses. Anything that depends on an endpoint
that was not mocked is a fixture artifact, not a defect. In particular the **Participants**
section reading "No players are attached to this campaign yet" is an artifact and is not
reported below. Every finding in this document was confirmed either in the DOM, in the
committed source, or in both.

## Measurements

These are the objective numbers the recommendations are based on. All were taken at a
1440 × 900 viewport unless stated.

| Measurement | Value |
| --- | --- |
| Campaign page height, GM, mid-campaign | 9,471 px (10.5 viewports) |
| Create-campaign page height, empty form | 23,518 px (26 viewports) |
| Create-campaign form controls | 242 |
| Create-campaign collapsible sections, all expanded by default | 57 |
| Campaign page collapsible sections, all expanded by default | 14 |
| Campaign page focusable elements | 135 (64 buttons, 42 form controls) |
| Median vertical gap between adjacent paragraphs inside a panel | 104 px |
| Vertical space above the `h1` on the campaign page | 426 px (47% of the viewport) |
| Vertical space above content on a 390 × 844 phone | ~320 px (38% of the viewport) |
| Map territories rendered / reachable by keyboard | 12 / 0 |
| `th` elements / `th` with `scope` | 9 / 0 |
| Elements exposing `aria-current` | 0 |
| Declared `color-scheme` | none (`normal`) |
| Primary button contrast, light theme | 7.76:1 (passes) |
| Primary button contrast, dark theme | 1.48:1 (fails AA, needs 4.5:1) |
| Danger button contrast, dark theme | 1.93:1 (fails AA) |

### Automated accessibility results

axe-core is close to clean, which reflects the ESLint template-accessibility rules already in
place. Four distinct violations were found across all pages:

| Rule | Impact | Where |
| --- | --- | --- |
| `aria-prohibited-attr` | serious | 12 × `polygon[aria-label]` with no role, in the map overlay |
| `aria-allowed-attr` | critical | `textarea[aria-expanded]` in the chat composer |
| `label` | critical | one unlabelled `input[type=file]` on the create-campaign page |
| `heading-order` | moderate | create-campaign page jumps `h1` → `h3` |

A clean automated run does not mean the pages are usable with a keyboard or a screen reader.
The most serious accessibility problems found here (`UI-C2`, `UI-C3`) are invisible to axe.

## What already works well and should be preserved

Call these out so they are not lost during refactoring.

- The skip link, the `:focus-visible` outline using `--color-glow`, and the `--touch-min: 44px`
  target size are all correct and applied globally.
- The two-step "Remove player" → "Confirm remove" pattern in `campaign-detail.page.html` is a
  good destructive-action pattern. Recommendation `UI-C4` proposes reusing exactly this.
- The shared saving overlay is the only dialog in the app that is correctly modal
  (`role="alertdialog"`, `aria-modal="true"`).
- Draft copy such as "No draft saved yet. If time runs out, this force Holds." is clear and
  states the consequence. More of the interface should read like this.
- The design-token layer in `styles.css` is a sound foundation. Most visual recommendations
  below add tokens rather than replacing the approach.
- Light-theme contrast is comfortably above AA everywhere it was measured.
- Territory selection by pointer is solid on both the campaign page and the map editor. Clicking
  or tapping a territory selects it and populates the details panel, and the `.territory-link`
  buttons scattered through the page (adjacent territories, force locations) call
  `selectTerritoryOnMap()`, which selects the territory, opens the map section, and scrolls it
  into view. That cross-referencing pattern is genuinely good and `UI-C2` builds its keyboard
  alternative on top of it rather than replacing it.

---

# Critical

Blocks a user, or fails WCAG 2.1 AA on a core path.

## UI-C1 — Primary and danger buttons are unreadable in dark mode

**Status:** implemented (2026-08-30). `--color-on-accent` and `--color-on-danger` tokens, contrast
tests in `theme-tokens.spec.ts`.

**Areas:** Accessibility, Visual design, Consistency

White button text is hard-coded while the accent and danger colors flip to light tints in the
dark theme. Measured in the running app:

- `.button`: `#fff` on `#5eead4` = **1.48:1** (AA requires 4.5:1)
- `.button-danger`: `#fff` on `#fca5a5` = **1.93:1**

Every primary action in the app is affected in dark mode, including Send, Commit Actions,
Surrender, Save schedule, and Save Map.

**Where:** `src/MapAndMuster.Web/src/styles.css` lines 158–187.

**Fix:** introduce an on-accent token per theme instead of hard-coding `#fff`.

```css
:root,
[data-theme='light'] {
  --color-on-accent: #fff;
  --color-on-danger: #fff;
}

[data-theme='dark'] {
  --color-on-accent: #06251e;
  --color-on-danger: #3f0a0a;
}

.button {
  background: var(--color-accent);
  color: var(--color-on-accent);
}

.button-danger {
  background: var(--color-danger);
  color: var(--color-on-danger);
}
```

`#06251e` on `#5eead4` is 12.1:1 and `#3f0a0a` on `#fca5a5` is 8.6:1.

**Verify:** a Vitest or Playwright assertion that computed contrast for `.button` and
`.button-danger` is at least 4.5:1 under both `data-theme` values.

## UI-C2 — The map cannot be operated with a keyboard

**Areas:** Accessibility, Map usability

The campaign map is the primary interface of the product, and it works only for users who can
operate a pointer.

**What already works, and is not the problem here.** Pointer and touch users are well served.
Clicking or tapping a territory selects it and fills the details panel, because
`onTerritorySelect` sets both `selectedIds` and `hoveredTerritoryId`, and the panel reads
`hoveredTerritoryId() ?? selectedIds().at(-1)`. The territory-name buttons rendered as
`.territory-link` (adjacent territories, force locations, battle locations) call
`selectTerritoryOnMap()`, which selects the territory, expands the map section, and scrolls the
map into view. The map editor has its own click-driven selection with a properties panel for the
selected territory. None of that needs changing; the keyboard alternative below should reuse it.

**The actual defect.** Selection is reachable by pointer only.

- 12 territory hit polygons are rendered; **0** are focusable. There is no `tabindex`, no
  `role`, and no key handling on them. They carry only `(pointerenter)` and `(pointerleave)`;
  activation is delegated from a `pointerdown` handler on the SVG root via `data-id`.
- The polygons carry `aria-label` but no role, so assistive technology discards the name
  entirely. This is the `aria-prohibited-attr` violation, 12 nodes.
- The viewport `role="application"` label documents pan and zoom keys, which do work, but
  selection, inspection, and move targeting have no keyboard path.
- The details panel's empty text, "Hover a territory to see its details.", understates what the
  panel supports and points users at the one input method that is hardest to discover on touch.

**Where:** `src/MapAndMuster.Web/src/app/shared/campaign-map-view/campaign-map-view.component.html`
lines 105–131; `src/MapAndMuster.Web/src/app/features/campaign-detail/campaign-detail.page.html`
line 551.

**Fix:**

1. Give each territory hit polygon `role="button"`, `tabindex="0"`, an `aria-pressed` or
   `aria-current` state for selection, and `keydown` handling for Enter and Space that calls the
   same delegated handler as `pointerdown`.
2. Have focus set `hoveredTerritoryId` the same way `pointerenter` does, so tabbing through
   territories drives the existing details panel with no change to how it computes its subject.
3. Reword the empty state to "Select a territory to see its details." to match the behavior that
   already exists.
4. Make the details panel an `aria-live="polite"` region so the name, terrain, owner, structure,
   and forces are announced on selection.
5. Add a keyboard-reachable territory list as a companion to the map, ordered by display number,
   whose buttons call the existing `selectTerritoryOnMap()`. This is the accessible alternative
   required by `src/MapAndMuster.Web/AGENTS.md` ("Build accessible keyboard alternatives for
   map-only interactions"), and it is a generalization of the `.territory-link` pattern the page
   already uses rather than a new mechanism.

**Verify:** a Vitest test asserting every `polygon.territory-hit` is focusable and exposes a role
and a name; a Playwright test that tabs to a territory, presses Enter, and asserts the details
panel updates. Add a regression test that clicking a `.territory-link` still selects the
territory and scrolls the map into view, so the existing behavior is not lost during the change.

## UI-C3 — Dialogs are not modal, Escape does not close them, and focus is not trapped

**Status:** implemented (2026-08-30). Shared `AppDialogComponent` with `aria-modal`, focus trap,
Escape/backdrop cancel, and inert on `.app-shell`.

**Areas:** Accessibility, Error prevention, Consistency

There are six hand-rolled dialogs. Only the saving overlay sets `aria-modal`. Measured against
the **End campaign** dialog, which is the most destructive action in the product:

- `aria-modal` is absent, so screen readers continue to expose the whole page behind it.
- Pressing Escape does not close it.
- After four Tab presses focus has left the dialog and is on page content behind it.
- Focus is not moved to the dialog on open, and it is not restored to the trigger on close.

**Where:**

| Dialog | File |
| --- | --- |
| Confirm action | `features/campaign-detail/campaign-detail.page.html` line 2026 |
| End campaign | `features/campaign-detail/campaign-detail.page.html` line 2044 |
| Join campaign | `shared/campaign-list/campaign-list.component.html` line 92 |
| Export log | `shared/campaign-log/campaign-log.component.html` line 27 |
| Download map | `features/map-editor/map-editor.page.html` line 39 |
| Save preset | `shared/save-campaign-preset-dialog/save-campaign-preset-dialog.component.html` line 5 |

**Fix:** add one shared `AppDialogComponent` in `shared/` that owns the backdrop, the panel, and
the behavior, then convert all six call sites to it. The component should:

- set `role="dialog"` (or `alertdialog` for confirmations) and `aria-modal="true"`;
- move focus to the dialog heading, or to the safe action, on open;
- trap Tab and Shift+Tab within the dialog;
- close on Escape and on backdrop click, emitting a `cancelled` event;
- restore focus to the invoking element on close;
- apply `inert` to `.app-shell` while open, matching what the saving overlay already does.

Default focus must land on the **safe** action. In the delete dialog, focus currently lands on
the red `button-danger`; it should land on Cancel.

**Verify:** Vitest coverage on `AppDialogComponent` (Escape, Tab cycle, focus restore, backdrop
click) plus conversion assertions on each of the six call sites. A Playwright pass over a live
dialog remains useful when the axe suite is expanded in step 3.

## UI-C4 — Irreversible actions fire on a single click with no confirmation

**Status:** implemented (2026-08-30). Shared `ConfirmButtonComponent`; Surrender is `button-danger`
and separated from Submit retreat.

**Areas:** Error prevention, GM interface, Campaign workflow

Several actions that cannot be undone execute immediately, and some of them are styled as safe
primary actions.

| Action | Where | Current styling | Consequence |
| --- | --- | --- | --- |
| Surrender | `campaign-detail.page.html` line 1345 | `class="button"` (primary green) | Concedes the engagement permanently |
| Leave campaign | `campaign-list.component.html` line 74 | `class="button-secondary"` | Removes the player from the campaign |
| Clear Connections | `map-editor.page.html` line 125 | `class="button-secondary"` | Discards every adjacency on the map |
| Remove Colors | `map-editor.page.html` | `class="button-secondary"` | Discards all manual territory colors |

The Surrender case is the most dangerous. "Submit retreat" and "Surrender" are rendered as two
adjacent, identically sized, identically colored primary buttons (lines 1342 and 1345), and both
read from the same destination select. Retreating is a normal tactical move; surrendering
concedes. A mis-click is easy and unrecoverable.

**Fix:**

1. Restyle Surrender as `button-danger` and separate it from Submit retreat with a rule or a
   distinct group so the two are not adjacent peers.
2. Apply the existing two-step confirm pattern already used by "Remove player" to Surrender,
   Leave campaign, Clear Connections, and Remove Colors. Extract it as a small shared
   `ConfirmButtonComponent` so it is consistent and testable.
3. For Surrender specifically, name the consequence in the confirmation text, for example
   "Surrender Windmere to Thornwild Clans? This cannot be undone."

**Verify:** Vitest: a single click on Surrender, Leave, Clear Connections, Remove Colors, and Remove
player does not perform the action; the second click does. Playwright coverage for Surrender is
deferred until a live battle fixture exists.

## UI-C5 — Invalid ARIA on the chat composer, the map, and the setup form

**Status:** implemented (2026-08-30), except map `polygon` labels which remain `UI-C2`.

**Areas:** Accessibility

Four automated violations, all fixable without behavior change.

1. **`aria-expanded` on a `textarea`** — critical. `aria-expanded` is not allowed on a plain
   textbox. The mention autocomplete needs the full combobox pattern: `role="combobox"`,
   `aria-autocomplete="list"`, `aria-controls` pointing at the suggestion listbox, and
   `aria-activedescendant` tracking the highlighted option. Without it, screen-reader users get
   no indication that suggestions appeared.
2. **`aria-label` on `polygon`** — serious, 12 nodes. Resolved by `UI-C2`.
3. **Unlabelled file input** — critical. One of the 14 file inputs on the create-campaign page
   has neither an associated `label[for]` nor an `aria-label`.
4. **Heading order** — moderate. The create-campaign page goes `h1` straight to `h3`
   ("Ranking public objectives"). The section legends between them are buttons inside
   `legend` elements rather than headings, so the outline has a hole. Promote the nested
   groupings to `h2`, or demote the `h3`s to match.

**Verify:** add axe-core to the existing Playwright suite and assert zero violations on the
login, campaign list, campaign detail, campaign setup, and map editor routes.

---

# High

Significant friction on a core path for a common role.

## UI-H1 — The campaign page has no "what do I need to do now" summary

**Areas:** Information hierarchy, Campaign workflow, Feedback

The campaign page is 9,471 px tall with all 14 sections expanded by default. The three things a
player actually needs on arriving — how long until the deadline, whether their orders are
committed, and who the campaign is waiting on — are scattered:

- The phase and countdown are three lines of unstyled body text about 1,100 px down the page.
- Commit state is only inferable from the Commit button deep inside **Actions**.
- "Waiting on" is an unstyled `<ul>` under an `h3` called **Commitments**, below the order forms.

**Fix:** add a status bar directly beneath the `h1`, above every collapsible section, that is
sticky on scroll and contains:

- round and phase ("Round 3 · Action 1");
- the countdown, with an urgency treatment when under a threshold (for example, switch to
  `--color-dirty` under 24 hours and `--color-danger` under 2 hours);
- the viewer's own state as a chip: "Not committed" / "Committed";
- a compact commitment roster ("2 of 4 committed") that links to the Commitments block;
- a primary "Go to your orders" action that scrolls to and expands **Actions**.

Keep the countdown in an `aria-live="polite"` region but throttle announcements so it does not
speak every second.

## UI-H2 — The campaign log occupies the top of the campaign page

**Areas:** Information hierarchy, Navigation

The first content on the campaign page, for players and GMs alike, is the campaign log and chat
composer. Status, map, and orders all sit below it. For a player whose task is "submit orders
before the deadline", the page opens on a conversation.

**Fix:** reorder the campaign page to match the task order:

1. Status bar (`UI-H1`)
2. Actions (your orders)
3. Campaign log (chat and history), with unread indicators for mentions and private messages
4. Map
5. Battles
6. Campaign points (Standings)
7. Reference sections (Campaign details, Factions, Ally groups, Links, Item objectives)
8. Management sections (`UI-M3`)

Default expanded sections while the campaign is running: **Actions**, **Chat**, and
**Standings**. Persist each user's expand/collapse choices per campaign. "Expand All" and
"Collapse All" stay.

## UI-H3 — Form controls ignore the theme

**Status:** implemented (2026-08-30). `color-scheme`, themed `.field` backgrounds, and `accent-color`.

**Areas:** Visual design, Consistency, Accessibility

`.field input`, `.field select`, and `.field textarea` set `border`, `border-radius`, and
`min-height` but never set `background` or `color`, and no `color-scheme` is declared on the
document. Measured computed background in **both** themes: `rgb(255, 255, 255)` with black text.
In dark mode every text field, select, and textarea is a stark white block on a `#1c1917` page.
Native checkboxes likewise render in the operating-system blue (`accent-color: auto`), which is
off-palette in both themes.

**Where:** `src/MapAndMuster.Web/src/styles.css` lines 125–139.

**Fix:**

```css
:root,
[data-theme='light'] {
  color-scheme: light;
}

[data-theme='dark'] {
  color-scheme: dark;
}

.field input,
.field select,
.field textarea {
  background: var(--color-surface);
  color: var(--color-text);
  accent-color: var(--color-accent);
}
```

Re-check `.field.invalid`, which sets `background: var(--color-error-bg)`; on dark that becomes
`#450a0a` and needs its text color checked.

## UI-H4 — Vertical rhythm wastes roughly half the page

**Status:** implemented (2026-08-30). Spacing scale, paragraph reset inside `.page`/`.stack`/`.panel`,
and Campaign details as a two-column `dl.facts`.

**Areas:** Visual design, Information hierarchy

The median gap between two adjacent paragraphs inside a panel is **104 px**. The cause is that
`.stack` uses `gap: 0.85rem` while the browser's default `<p>` margins are still in effect, so
each paragraph contributes 16 px top and 16 px bottom on top of the grid gap, and grid margins do
not collapse. The **Campaign details** section is nine single-line facts spread over roughly
1,000 px.

**Where:** `src/MapAndMuster.Web/src/styles.css`, `.stack` at line 111. There is no global
paragraph reset; the only `p { margin: 0 }` is scoped to the banner classes at line 238.

**Fix:**

1. Reset paragraph margins globally and let `.stack` own the spacing:
   `.stack > p, .panel p { margin: 0; }`
2. Add a spacing scale so component CSS stops inventing values:
   `--space-1: 0.25rem` … `--space-6: 2rem`, and use it in `.stack`, `.panel`, and `.field`.
3. Render label/value groups such as **Campaign details** as a two-column `<dl>` rather than a
   stack of paragraphs.

This alone should cut the campaign page height by roughly 40% with no content removed.

## UI-H5 — Campaign cards show only a name

**Areas:** Navigation, Information hierarchy, Campaign workflow

A collapsed campaign card renders the campaign name and nothing else. Status, round, phase,
countdown, player count, and the **Open** button are all inside the expanded body. On "Your
campaigns" an active campaign with a running deadline is visually identical to one that finished
in May.

**Where:** `shared/campaign-list/campaign-list.component.html` lines 21–29.

**Fix:** put the decision-making data on the collapsed card:

- a status chip (Scheduled / In progress / Completed);
- for in-progress campaigns, "Round 3 · Action 1" and the countdown;
- "4 of 8 players";
- the role badge (Player / Manager) already computed by `roleLabel()`;
- **Open** as a primary action on the card itself, so reaching a campaign is one click.

Keep the disclosure for description, location, dates, and the secondary actions.

## UI-H6 — "Edit map" is offered for in-progress campaigns and silently bounces

**Areas:** GM interface, Feedback, Error prevention

`campaign-detail.page.html` line 22 renders **Edit map** whenever the viewer is staff. The
adjacent **Edit campaign** at line 20 is correctly gated on `campaign.status === 'Scheduled'`.
The map editor itself redirects to `/play` when status is not `Scheduled`
(`map-editor.page.ts` line 1296), with no message. A GM on a running campaign clicks Edit map
and is returned to the page they were already on, with no explanation.

**Fix:** gate the link the same way `Edit campaign` is gated. If the map genuinely must be
reachable while a campaign runs, keep the link but land on a read-only map view that states why
editing is closed. Either way, replace the silent redirect with a message.

## UI-H7 — Battle reminders and the hidden-relic notice render twice

**Areas:** Consistency, Information hierarchy

`campaign-detail.page.html` renders the same two pieces of information twice in each force card:

```
775  @if (force.hiddenRelicNearby) { <p …>A hidden relic is in an adjacent territory.</p> }
778  @if (force.battleReminders?.length) { <ul> … <li …>{{ reminder }}</li> … </ul> }
785  @if (force.hiddenRelicNearby) { <p …>A hidden relic is in an adjacent territory.</p> }
788  @for (reminder of force.battleReminders ?? []; …) { <p …>{{ reminder }}</p> }
```

Every player with a special rule or an adjacent hidden relic sees the text duplicated, once as a
list and once as paragraphs.

**Fix:** delete lines 785–790 and keep the list form. Add a Vitest case asserting a force with
one reminder and `hiddenRelicNearby` renders each string exactly once.

## UI-H8 — The create-campaign page is one 26-viewport form

**Areas:** GM interface, Information hierarchy, Error prevention

A GM creating their first campaign is shown 23,518 px of form containing 242 controls across 57
collapsible groups, all expanded, before typing anything. Only 19 controls are marked required.
The mandatory subset is small: name, schedule, factions, terrain, and a map.

**Fix (presentation only, no change to the submitted payload):**

1. Collapse every optional section by default and expand only the four marked `*`
   (**Campaign details**, **Schedule**, **Factions**, **Terrain types**) plus **Campaign map**.
2. Add a sticky section index down the left at wide viewports, listing the sections with a
   completion state, so a GM can see the shape of the task and jump around.
3. Show a "required fields remaining" count in the sticky toolbar next to Create campaign.
4. Fix the heading outline (`UI-C5`, item 4) so the index and screen-reader outline agree.
5. The nested per-terrain and per-structure **Missions** sections are 18 identically named
   groups. Name them "Missions for Beach", etc., so the accessible name is unique.

## UI-H9 — The standings table encodes faction by color alone and has no table semantics

**Status:** implemented (2026-08-30).

**Areas:** Accessibility, Information hierarchy, Consistency

In **Campaign points**:

- The Faction column is a bare colored square. There is no name and no text alternative, so the
  column carries no meaning for anyone who cannot distinguish the colors. This is a WCAG 1.4.1
  failure.
- None of the 9 `th` elements carry `scope`, so column and row association is guesswork for
  screen readers.
- The viewer's own row is not distinguished.
- The **Total** column has the same weight as its five components.
- The **Display name** column renders the display name and the username adjacently, producing
  "ada ada" whenever `displayNameMode` is `Username`.
- The header **Structures captured** sits above `territoryAndStructurePoints`, which also counts
  territories. The label understates what the number means.

**Fix:** add `scope="col"` to headers and `scope="row"` to the name cell; render the faction name
next to the swatch; emphasize Total; mark the viewer's row with a left border and a visually
hidden "you"; suppress the duplicate username when it equals the display name; rename the column
to **Territories and structures**.

## UI-H10 — Raw enum values are shown to users

**Areas:** Consistency, Feedback

Battle headings render the status enum unmodified, producing `Windmere · AwaitingResults`. This
was confirmed in the live heading outline.

**Fix:** add a display-name pipe or map for battle status, force status, phase kind, and
campaign status, and use it wherever these appear. "Awaiting results". Add a Vitest case per
enum value so a new member cannot leak a raw name.

## UI-H11 — Mobile spends 38% of the screen on chrome before any content

**Areas:** Responsive design, Navigation

At 390 × 844:

- The banner takes about 130 px and the nav wraps to four rows for about 190 px, so roughly
  320 px is consumed before the page title.
- The username sits alone on its own row in low-contrast grey.
- The page toolbar wraps to two more rows.
- The campaign log is monospace and wraps mid-timestamp, producing four-line fragments such as
  "(August 22, 2026, 11:31:00 AM EDT) Campaign: No order was submitted for Longbarrow."
- Nothing about the deadline or the player's pending order is visible on the first screen.

**Fix:**

1. Below roughly 720 px, collapse the primary nav behind a single menu button, keeping Home and
   the theme toggle visible. Move the username into the menu.
2. Scale the banner down on small viewports (a compact wordmark rather than the full 1024 × 341
   artwork) and consider shrinking it on scroll at all sizes.
3. Drop the monospace font for the log below the same breakpoint, and move the timestamp to a
   secondary line rather than a prefix (see `UI-P4`).
4. With `UI-H1` in place, ensure the status bar is the first thing under the title on phones.

## UI-H12 — The active navigation item is signalled by color alone

**Status:** implemented (2026-08-30).

**Areas:** Accessibility, Navigation

`aria-current` appears zero times in the rendered document. `routerLinkActive` applies an
`is-active` class that changes the background only, so the current page is not exposed to
assistive technology and is weak for anyone with low color discrimination.

**Where:** `src/MapAndMuster.Web/src/app/app.html` lines 16–36.

**Fix:** add `[attr.aria-current]="rla.isActive ? 'page' : null"` using a `routerLinkActive`
template reference on each nav link, and add a non-color indicator such as an inset bottom
border to `.nav-link.is-active`.

## UI-H13 — Commit can be irreversible, and the UI promises the opposite

**Areas:** Error prevention, Feedback, Campaign workflow

`AGENTS.md` and `docs/CAMPAIGN-RULES-MATRIX.md` both state that the final required commitment
closes planning atomically, and that uncommit is allowed only while the action window is open.
So the player who happens to commit last does not get an uncommit window at all — their commit
resolves the phase immediately.

The interface does not reflect that anywhere:

- The **Commit Actions** button carries no warning. Before pressing it, a player has no way to
  know whether they are the last required commitment and are therefore about to close the phase
  for everyone.
- The reassurance copy is shown only *after* committing, and it is unconditional: "Uncommit
  returns them to draft until this action window closes." For the last committer that sentence
  is false at the moment it appears, and an **Uncommit** button is rendered next to it.
- The data needed to tell the difference is already on the client. The play payload includes
  `commitments[]` with `userId`, `username`, and `isCommitted`, so "everyone else has committed"
  is computable without any API change.

There is a **Commitments** list, so this is a matter of placement and wording rather than a
missing feature. It sits at the very bottom of the Actions accordion, after every force
fieldset, rendered as plain `username · Committed/Drafting` lines with no count or summary. On a
mid-campaign page that puts the answer to "who are we waiting on?" well below the Commit button
that depends on it.

**Where:** `src/MapAndMuster.Web/src/app/features/campaign-detail/campaign-detail.page.html`
lines 747–756 (commit/uncommit) and 948–962 (commitments list).

**Fix:**

1. Compute a `isFinalRequiredCommitment` signal from `play.commitments` — true when every other
   required player is already committed.
2. When it is true, change the button to **Commit Actions and close the phase**, and require the
   confirmation dialog from `UI-C3`, worded so the consequence is explicit: this closes planning
   for all players immediately and cannot be undone.
3. Make the post-commit copy conditional. Only promise uncommit when the action window is
   actually still open; otherwise state that the phase has closed and the orders are resolving,
   and do not render the **Uncommit** button.
4. Move a compact summary of the commitments list above the Commit button — "4 of 6 players
   committed. Waiting on northplayer, eastplayer." — and keep the full per-player list where it
   is. This answers the deadline question at the point of decision and gives the Feedback
   category the "waiting on players" signal it currently only supplies by scrolling.

**Verify:** a Vitest test that renders a play state where all other players are committed and
asserts the confirming label and dialog appear; a second test where at least one other player is
drafting and asserts the plain label and the uncommit promise. Add a test that a committed state
with a closed window does not render an **Uncommit** button.

---

# Medium

Worth doing, but a user can complete their task without it.

## UI-M1 — The map has no legend and no territory labels

**Areas:** Map usability, Information hierarchy

The map simultaneously uses four visual encodings — a faint fill tint for ownership, diagonal
hatching for spawn locations, colored flag pins for forces, and glyph pins for structures and
items — and explains none of them. Territory names are never drawn on the map, so identifying a
territory means hovering or selecting them one at a time and reading the details panel.

**Fix:** add a collapsible legend beside the map covering ownership tint, spawn hatching, force
pins, your own force, in-battle state, structures, pillaged structures, and item objectives. Add
a "Show names" toggle that draws the display number, or the name when it fits, at each territory
centroid. Raise the ownership fill opacity so ownership is readable at Fit zoom.

## UI-M2 — Order entry is hard to scan and the confirm control is unlabelled

**Areas:** Campaign workflow, Information hierarchy, Accessibility

Within a force card:

- The force identity ("ada · Ember Compact · Harrowgate") is plain body text, smaller and
  lighter than the "Action" field label beneath it, inverting the hierarchy.
- The control that applies the draft is a 40 px icon-only check button with no visible text, and
  its enabled and disabled states are hard to tell apart.
- **Commitments** is an `h3` followed by a plain `<ul>` of "name · state" strings.

**Fix:** promote the force identity to the card heading with the faction color as a left border;
give the confirm control a visible label ("Save draft") or at minimum a tooltip plus
`aria-label`, and make its disabled state unambiguous; render commitments as a roster with a
state chip per player and a "2 of 4 committed" summary.

## UI-M3 — Management controls are interleaved with play controls

**Areas:** GM interface, Error prevention

On the GM view, **Extend schedule**, **Debug**, **Edit map**, and **End campaign** sit in the
same undifferentiated stack of sections as **Actions**, **Battles**, and **Campaign points**.
`End campaign` is a top-level `h2` section like any other. A GM who is also a player has no
visual boundary between the moves they make as a player and the interventions they make as
staff, which is exactly the separation `PRODUCT.md` calls for.

**Fix:** group all manager-only sections under a single **Manage campaign** region, collapsed by
default, with a distinct surface treatment (a tinted background or a left accent border) and a
short line explaining that actions inside it are attributed and notified. Keep **End
campaign** last inside it.

## UI-M4 — Six hand-rolled dialogs

**Status:** implemented (2026-08-30). Shared `AppDialogComponent`; per-page `.dialog` / `.dialog-backdrop`
rules removed.

**Areas:** Consistency

Beyond the accessibility defects in `UI-C3`, the six dialogs duplicate backdrop and panel markup
and CSS across five files. Fixing any one of them today fixes only that one.

**Fix:** the shared dialog component proposed in `UI-C3` also removes this duplication. Delete
the per-component `.dialog` and `.dialog-backdrop` rules once it lands.

## UI-M5 — Button widths are inconsistent

**Status:** implemented (2026-08-30). `.stack >` buttons use `justify-self: start`.

**Areas:** Consistency, Visual design

Buttons in page toolbars size to their content, while buttons placed directly in a `.stack`
stretch to the full container width because `.stack` is a grid with default `stretch` alignment.
The result is a 1,000 px wide "Surrender" and a 1,000 px wide "Save schedule" next to normal
auto-width buttons elsewhere.

**Fix:** wrap standalone buttons in the existing `.actions` flex container, or add
`justify-self: start` for buttons that are direct grid children. A full-width button should be a
deliberate choice, not a side effect.

## UI-M6 — Empty states are dead ends

**Areas:** Empty states, Navigation

- "Your campaigns" when empty says "You are not managing or participating in any campaigns yet"
  and suggests creating one, but the only button on the page is **Create campaign** in the far
  top-right corner, and for a new player the correct first action is to join, not to create.
- "All campaigns" when empty explains the rule but offers no action.
- Home shows "No new notifications." and "No news has been published yet." and offers a player
  nothing at all.

**Fix:** put the primary action inside the empty state, next to the explanatory text, and choose
it for the role. For a player with no campaigns, the primary action is "Browse campaigns to
join" and "Create a campaign" is secondary.

## UI-M7 — Home is not a dashboard

**Areas:** Navigation, Information hierarchy, Campaign workflow

Home contains only Notifications and News. A returning player has to remember which campaign is
live, navigate to Your campaigns, expand the right card, and open it, before learning that a
deadline is four hours away.

**Fix:** add an "Needs your attention" block at the top of Home listing, per active campaign,
the round and phase, the countdown, and whether the viewer has committed, each linking straight
to that campaign's Actions section. Keep Notifications and News below it. This uses data already
returned by `GET /api/campaigns`.

## UI-M8 — Registration and profile are undifferentiated walls of fields

**Areas:** Visual design, Information hierarchy, Consistency

Both pages render every field full width in a single column, so a one-character **Middle
initial** gets the same 600 px as **Email**. Registration is twelve consecutive fields with no
grouping. On the profile page the `fieldset`/`legend` groupings are visually almost invisible,
and the notification checkboxes render far from their labels with the operating-system blue.
Both pages use unstyled native `input[type=file]` controls, which are the only browser-default
widgets in an otherwise custom-styled interface.

**Fix:** introduce a `.field-row` grid so related short fields share a row (First / Middle /
Last; City / State / Country); give `fieldset` a visible surface and a stronger legend; restyle
the checkbox rows so the control sits immediately before its label; add a styled file-input
component with a visible file name and a "Choose image" button; on the long profile form, make
the Save button sticky at the bottom.

## UI-M9 — The map editor toolbar is 21 undifferentiated controls

**Areas:** GM interface, Visual design, Error prevention

Above the canvas there are six page-header buttons and fifteen toolbar buttons, nearly all
`button-secondary` grey pills with no icons, grouping, or separators. Modes (Draw, Erase, Select,
Connect) look identical to one-shot commands (Undo, Save Map). Destructive commands (Erase,
Clear Connections, Remove Colors, Clear Unsaved Changes) look identical to safe ones. The active
mode uses the same dark green as a primary action, so "Draw is selected" and "this is the main
button" are the same signal.

**Fix:** render the four modes as a real radio group with icons and `aria-pressed`, visually
distinct from commands; separate command groups with rules and labels ("Connections",
"Colors", "File"); give destructive commands the danger treatment plus the confirm pattern from
`UI-C4`; pick a different token for active mode so it does not collide with the primary action
color. Also expand the right-hand **Territory** properties panel by default at wide viewports —
it is currently a collapsed 20 px strip.

## UI-M10 — Two different disclosure patterns, with the affordance far from the label

**Areas:** Consistency, Visual design

The campaign log uses a text `▼` immediately before its label. Every other section uses a `▾`
pushed to the far right edge of the panel, up to 1,000 px away from the heading it belongs to.
Campaign list group headings do the same.

**Fix:** standardize on one disclosure control — the `app-icon` chevron placed immediately
before the label, rotating on expand — and apply it to `.section-toggle`, the campaign log, and
the campaign list groups. Keep the whole header row clickable.

## UI-M11 — Battle cards are flat text

**Areas:** Information hierarchy, Campaign workflow

A battle awaiting results renders Mission, Attacker, Defender, and two per-force supply blocks as
a flat sequence of paragraphs separated by roughly 50 px each. Attacker and Defender — the two
facts that determine how the tabletop game is set up — are muted grey at body weight, less
prominent than the supply numbers below them.

**Fix:** lay the battle out as two opposed force columns with the attacker and defender roles as
chips at the top of each; put the supply figures in a small aligned table; and place the result
form in its own bordered group so reporting is clearly a separate step from reading the setup.

## UI-M12 — Site chat outranks the campaign list on "All campaigns"

**Areas:** Information hierarchy, Navigation

The page is titled "All campaigns" but opens with the Site chat panel and its composer. The
campaign list sits below it in an unlabelled panel with no heading of its own.

**Fix:** move Site chat below the campaign list, collapse it by default, and give the campaign
list an `h2`.

## UI-M13 — The test users page does not scale

**Areas:** Administrator experience, Visual design

Thirty rows, each with the account label on the far left and its "Test as this user" button
pinned to the far right about 570 px away, with no search, no filter, and no indication of which
accounts are in the current campaign.

**Fix:** constrain the row width so the label and its action stay visually associated, add a
filter box, and show whether each test account is currently a member of any campaign. Indicate
the currently impersonated account.

## UI-M14 — Delinquency reaches the GM as one log line, not as decision support

**Areas:** GM interface, Information hierarchy, Feedback

`docs/CAMPAIGN-RULES-MATRIX.md` requires the application to keep a per-force campaign-lifetime
count of missed drafts, missed required retreats, and missed results, and to notify managers
from the third offence as a possible kick, while never removing anyone automatically. The
decision to remove a player is therefore deliberately left to a human.

The backend implements this — `DelinquencyRules`, `PlayLogKind.DelinquencyThreshold`, and a
notification kind all exist. The web app never mentions it. The word "delinquen" does not appear
anywhere under `src/MapAndMuster.Web`. In practice a GM sees a single log entry, "X's force
reached three missed-order offences and may be kicked.", scrolling past in the campaign log
alongside every other event, and a notification. Meanwhile the **Remove player** control lives on
the Participants list with nothing next to it to justify pressing it, so the GM is asked to make
the judgement in a place that shows none of the evidence.

**Fix:** in the Participants list, badge any player whose force has crossed the threshold, and
link the badge to the corresponding log entry so the GM can read the history before acting. Give
the campaign log a filter for delinquency events, which fits the filtering already recommended in
`UI-M2`.

**Open question, out of scope for this audit.** A running per-player offence count next to each
participant would be the genuinely useful version of this, and it is what the rules matrix
describes. `PlayContracts.cs` does not currently expose the count, so that variant needs an API
contract change and falls outside the presentation-only boundary set at the top of this document.
Flagging it for a decision rather than recommending it.

---

# Polish

Low effort, low risk, improves finish.

## UI-P1 — The theme toggle labels the current state, not the action

The button reads "Light mode" while light mode is active. Users commonly read a button label as
what will happen when pressed. Use `aria-pressed` with a stable label, or label it with the
action ("Switch to dark mode").

## UI-P2 — Duplicated identity in standings

When `displayNameMode` is `Username`, the display name and the username link render side by side
as "ada ada". Suppress the link when the two strings match.

## UI-P3 — Unexplained "(0)" on ally groups

Ally groups render as `Northern Pact (0) - Ember Compact (Cinderguard, Forgewrights), Tidewatch
League`. The number is unlabelled. Either label it or drop it.

## UI-P4 — Log entries lead with a 30-character timestamp

Every line begins with `(August 22, 2026, 10:02:11 AM EDT)` before any content, so scanning the
log by subject is impossible. Move the timestamp to the end of the line, or to a right-aligned
secondary column, and consider relative times for recent entries with the absolute time in a
`title`.

## UI-P5 — The banner takes 240 px on desktop before anything else

Combined with the menubar and toolbar, 426 px — 47% of a 900 px viewport — sits above the page
title. Consider a shorter banner crop, or shrinking it to a compact wordmark on scroll.

## UI-P6 — The "Test users" nav link has no icon

Every other item in the primary nav pairs an `app-icon` with its label; the administrator link
does not. Add one.

## UI-P7 — Mixed casing between navigation and page titles

The nav uses "Your Campaigns" while the page heading is "Your campaigns". Both render in small
caps so the difference is invisible today, but it will surface anywhere the raw string is used.
Pick sentence case and apply it consistently.

## UI-P8 — The map does not fit on load

The map opens at 100% zoom, letterboxed with wide empty margins inside its panel. Default to
Fit, and remember the user's zoom per campaign.

## UI-P9 — Guard the map hover animation behind `prefers-reduced-motion`

Territory hover applies a lift transform. Wrap the transition in a
`@media (prefers-reduced-motion: no-preference)` block.

---

# Recommendations by category

| Area | Items |
| --- | --- |
| Navigation | `UI-H5`, `UI-H11`, `UI-H12`, `UI-M6`, `UI-M7`, `UI-M12` |
| Campaign workflow | `UI-C4`, `UI-H1`, `UI-H5`, `UI-H13`, `UI-M2`, `UI-M7`, `UI-M11` |
| Map usability | `UI-C2`, `UI-M1`, `UI-M9`, `UI-P8`, `UI-P9` |
| Information hierarchy | `UI-H1`, `UI-H2`, `UI-H4`, `UI-H5`, `UI-H8`, `UI-H9`, `UI-M1`, `UI-M2`, `UI-M7`, `UI-M8`, `UI-M11`, `UI-M12`, `UI-M14` |
| GM interface | `UI-C4`, `UI-H6`, `UI-H8`, `UI-M3`, `UI-M9`, `UI-M14` |
| Consistency | `UI-C1`, `UI-C3`, `UI-H3`, `UI-H7`, `UI-H10`, `UI-M4`, `UI-M5`, `UI-M8`, `UI-M10`, `UI-P7` |
| Visual design | `UI-C1`, `UI-H3`, `UI-H4`, `UI-M5`, `UI-M8`, `UI-M9`, `UI-M10`, `UI-M13`, `UI-P5` |
| Responsive design | `UI-H11`, `UI-P5` |
| Accessibility | `UI-C1`, `UI-C2`, `UI-C3`, `UI-C5`, `UI-H3`, `UI-H9`, `UI-H12`, `UI-M2`, `UI-P9` |
| Error prevention | `UI-C3`, `UI-C4`, `UI-H6`, `UI-H8`, `UI-H13`, `UI-M3`, `UI-M9` |
| Feedback | `UI-H1`, `UI-H6`, `UI-H10`, `UI-H13`, `UI-M14` |
| Empty states | `UI-M6`, `UI-M7` |

# Suggested implementation order

Grouped so that shared work lands before the items that depend on it.

1. **Tokens and global CSS.** `UI-C1`, `UI-H3`, `UI-H4`, `UI-M5`. Done 2026-08-30. One change
   to `styles.css` plus a sweep of component CSS. Everything else sits on top of this.
2. **Shared primitives.** The dialog component (`UI-C3`, `UI-M4`) and the confirm-button
   component (`UI-C4`), then convert the six dialogs and four destructive actions. Done 2026-08-30.
3. **Accessibility corrections.** `UI-C5`, `UI-H9`, `UI-H12`, and the axe assertions in the
   Playwright suite. Done 2026-08-30. Map `polygon` labels stay excluded until `UI-C2`.
4. **Map accessibility.** `UI-C2`, then `UI-M1`.
5. **Campaign page restructure.** `UI-H1`, `UI-H2`, `UI-H13`, `UI-M2`, `UI-M3`, `UI-M11`,
   `UI-H7`, `UI-H10`. `UI-H13` depends on the dialog work in step 2.
6. **Entry points.** `UI-H5`, `UI-M6`, `UI-M7`, `UI-M12`.
7. **Setup and editor.** `UI-H6`, `UI-H8`, `UI-M9`, `UI-M14`.
8. **Responsive.** `UI-H11`, `UI-M8`, `UI-M13`.
9. **Polish.** `UI-P1` through `UI-P9`.

# Constraints for whoever implements this

- Custom dialogs and confirm buttons, not a component library. See DECISIONS-NEEDED item 20.
  Campaign list items expose `canChooseFaction`, `isCommitted`, and `currentPhaseKind`.
  Campaign-log last-read is persisted (`GET /log` unread fields and `POST /log/read`). Card
  badges, Home dashboard chrome, and chat unread indicators still follow in later UI steps.
  Playwright axe scans login, campaign list, campaign detail, campaign setup, and the map
  editor. Map `polygon` nodes stay excluded until `UI-C2`.
- Nothing here changes a campaign rule, an authorization decision, or persisted play state. If an
  implementation appears to need one, stop and raise it rather than proceeding.
- Client-side validation and disclosure remain presentation only. Hiding a control is never an
  authorization decision; see `src/MapAndMuster.Web/AGENTS.md`.
- Every behavioral UI change needs a Vitest update in the same commit, and the secrecy and
  workflow paths need Playwright coverage. See `docs/TESTING-STRATEGY.md`.
- Do not weaken ESLint, Stylelint, or template strictness to land any of this.

# Open questions for the product owner

Resolved 2026-08-30. See `docs/DECISIONS-NEEDED.md`.

1. Resolved: any signed-in player can create a campaign. Empty "Your campaigns" should offer
   **Join campaign** as a shortcut to All campaigns, with **Create a campaign** remaining available.
   Joined campaigns with remaining setup choices (for example faction selection) show an indicator
   on the card, and the same items appear in the Home notifications list (`UI-M6`, `UI-M7`).
2. Resolved: the map is not editable after a campaign starts. Hide **Edit map** when status is
   not `Scheduled`, and replace the silent redirect with a message if the editor is reached anyway
   (`UI-H6`).
3. Resolved: while a campaign is running, default expanded sections are **Actions**, **Chat**, and
   **Standings**. Persist each player's last open/closed set per campaign (`UI-H2`).
4. Resolved: page order is **Orders (Actions)**, then **Chat log**, then **Map**. The chat log is
   the main social surface and campaign history. Unread mentions and private messages show unread
   indicators on the chat log (`UI-H2`).
