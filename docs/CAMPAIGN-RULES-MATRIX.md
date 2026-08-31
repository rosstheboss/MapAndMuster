# Campaign Rules Classification

Source reviewed: _The Hunt in Estalia Campaign_, draft dated October 2026, 26 pages.

This document translates the supplied campaign draft into software responsibilities without
copying game-specific prose into the generic application. It is analysis, not a replacement
for the campaign rules.

## Classification key

- **Enforce:** reject or transform invalid commands on the authoritative server.
- **Calculate:** derive state, resources, points, or assignments.
- **Secret:** apply field/record-level authorization and prevent client over-fetching.
- **Audit:** preserve actor, reason, time, before/after, and revision.
- **Configure:** campaign staff provide structured data/rules.
- **Display:** show reference text; do not simulate tabletop behavior.
- **TBD:** incomplete or ambiguous; agents must not invent it.

## Application rule matrix

| Rule area                            | Classification                | Initial software responsibility                                                                                                |
| ------------------------------------ | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| Campaign length and round schedule   | Configure, Enforce, Audit     | Configure rounds/deadlines and per-phase "End phase early if able to resolve" (default on); after launch allow round-count increase and lengthening remaining windows, not shortening them. |
| Two action windows and battle phase  | Enforce                       | Maintain explicit timed states; Action 1 battle can force later Battle action. When early-close is off, a battle phase stays open until its deadline so a GM can inject a ringer fight. |
| Draft, commit, uncommit, early close | Enforce, Audit                | A confirmed map action or force-panel checkmark saves a draft. Commit is allowed only after every required force has a saved draft. The latest draft commits at the deadline or on commit. Uncommit back to draft only while the action window is open. Close atomically when early-close is on. Unused battle time flows into the next window. |
| Campaign play log                    | Audit, Display                | Show a log on the campaign page (upcoming through completed) with timestamps and originators; load that log separately from campaign metadata so chat does not wait on the rest of the page; record campaign start, campaign close, schedule extensions, revealed actions including Hold, invalid or conflicting submitted actions that became Hold, battles, manager battle-result overrides, retreats, and automatic force rejoins; accept member chat to public, direct, faction, or ally-group channels with `@` tags of current members only; let composers type and autocomplete Everyone or a member username; link chat originators and mentions to public profiles; omit private chats from unauthorized payloads, including campaign managers; never log unrevealed orders or site-wide chat; let a manager or administrator download public chat and/or game-log facts as one text or CSV file at any lifecycle stage, omitting private chat. |
| Site chat                            | Display, Notification         | Show a public site-wide chat on All Campaigns, separate from campaign logs; allow `@` tags of any account; reject prohibited language; let users block one another so player messages hide both ways; let administrators announce to everyone or one person with notifications; flag and filter languages without translating. |
| Participants and public profiles     | Display, Secret               | List attached members with faction and Manager/Player/Admin roles; open public profiles from member names; list only campaigns the viewer may already view. |
| Notifications and home board         | Notification, Display         | Notify in-app and/or by email of mentions, private chats, campaign start, campaign end, new phases, actions that still need the user, site-chat mentions, administrator site-chat announcements, and manager delinquency kick recommendations from the third offence; show those items on Home, or "No new notifications." when none remain; email bodies omit hidden orders, private chat text, and site-chat bodies. |
| Site news                            | Display                       | Show administrator-authored markdown news on Home, one article per page. |
| Missing order                        | Enforce                       | Create Hold for every missing required force/action slot. A force with no draft at all is a delinquency offence; an uncommitted draft is not. |
| Move                                 | Enforce, Calculate            | Validate controller, origin, destination, adjacency, spawn restrictions, and faction modifiers. Invalid Move becomes Hold.     |
| Hold                                 | Enforce, Calculate            | Preserve location and trigger configured rest/status effects.                                                                  |
| Build                                | Enforce, Calculate            | Validate structure slot and a buildable type; create owner/controller state.                                                   |
| Pillage                              | Enforce, Calculate            | Owner may pillage own structures; allies cannot. Validate pillageable/destructible flags and battle precedence; progress or remove the structure. |
| Repair                               | Enforce, Calculate            | Current territory owner or a current ally of that owner may restore a pillaged structure.                                       |
| Split/rejoin forces                  | Enforce, Calculate, Audit     | Enforce force limit and target eligibility; independently track split forces; automatically rejoin co-located same-player forces into one action and log the rejoin. |
| Backstab                             | Enforce, Calculate, Audit     | Break the alliance. Empty former-ally land: claim and auto-pillage (never auto-destroy). Shared land: create a battle with no auto-pillage. |
| Retreat                              | Enforce, Calculate, Audit     | Validate eligible destinations, resolve collisions by strongest (campaign points, territories, structures, supply including temporary, then recorded random), and use spawn fallback. |
| Automatic Battle                     | Calculate                     | Create engagement when enemy forces remain together after precedence resolution.                                               |
| Surrender                            | Enforce, Calculate, Audit     | Allowed while engaged in an action or battle window. Committed surrender cannot be withdrawn; a draft still executes at the deadline. 1v1 awards max VP battle points with no extra BP. Remaining side wins if everyone else runs; mutual run is a no-contest. |
| Multiple combatants                  | Calculate                     | Allied extras add 25 percent round army points per extra player, split evenly, round up to 10. More than two opposing sides: two strongest play first, then strongest-to-weakest in the same phase; unplayed forces stay for the next battle phase. A correction joiner waits, then plays remaining forces in that sequence. |
| Allied co-location                   | Calculate                     | Allies cannot claim allied land without backstab. Two allies on Neutral: strongest claims. Allied occupants defend without taking the flag. |
| Map image and territories            | Configure                     | Upload raster, draw/import polygons, assign IDs, terrain, ownership, spawn, structures, structure condition, and adjacency. A pair of territories has at most one connection. |
| Spawn behavior                       | Enforce, Calculate            | Prohibit enemy entry/battle/build, provide base supply, combine same-player split forces, and drop carried item objectives on the territory a force leaves. |
| Terrain types                        | Configure, Display            | Select mission/layout candidates and show tabletop rules. Standard terrain is a replaceable preset copied from the current catalog. Each type has a Water feature flag for special-rule interaction and player reminders; it does not change movement by itself. Terrain and structure mission lists attach catalog or one-off missions. |
| Mission assignment                   | Configure, Calculate, Display | Structure missions if any, otherwise terrain. Prefer attacker/defender missions for Hold/Retreat vs Move/Split, owned-structure or allied defense, or backstab; otherwise only when no normal mission remains. Show the chosen mission (and file/URL) on the campaign battle panel. Army-point advantages clamp at 500; supply advantages clamp at 1. |
| Tabletop terrain features            | Display                       | Present woods, water, river, bridge, cliff, castle, and similar rules with the mission; do not simulate tabletop movement.     |
| Supply network                       | Configure, Calculate, Display | Graph traversal from spawn through owned or allied (same group, not backstabbed) territories. Terrain and operational owned or allied structures grant configured supply points (default 1) to a connected force. Structure campaign points stay with the territory owner. Show current supply on the Participants list and on battles to resolve, with standard vs temporary amounts and the army-point cap for that game. |
| Temporary supply                     | Configure, Calculate, Audit   | Award configured pillage/destroy points (default 1) to the earning player. That player may spend the pool on any of their forces; each point applies to exactly one force. Battle reports spend supply-costing units from force allowance first, then this pool. |
| Split-force supply                   | Configure, Calculate          | Each split force claims the same territory/structure supply minus the split penalty (Hunt default raw 1, or a configured 0–100 percent), minimum 1 each, plus the full round free-supply bonus. |
| Army escalation                      | Configure, Calculate          | Configure one row per round for max army points (10–100000), free supply, and free characters. Generic default is 1000/1/1; Hunt in Estalia uses its eight-round table. |
| Battle result reporting              | Configure, Enforce, Audit     | Players report both sides. Agree, dispute, staff confirm, or deadline-commit. One timely submission is authoritative. Neither submission is a no-contest forced retreat (skip Shaken, no spoils). True BP ties force both to retreat without a loss. |
| Army-list submission                 | Configure, Display            | Optional pasted list text is stored on the report and shown to the participant, opponent, and staff. Warhammer: The Old World lists from New Recruit or Old World Builder may auto-fill category supply amounts; Other and unrecognized text leave amounts to the player. Full army-list legality is outside scope. |
| Campaign presets                     | Configure                     | Built-in Hunt in Estalia catalog plus administrator-saved named presets (from Edit campaign or Edit map) that include map image and overlay graph, remapped by catalog name on apply. Duplicate names after trimming whitespace overwrite. Administrators may download/upload a portable preset ZIP between hosts. |
| Army composition                     | Calculate, Display            | Calculate campaign-level supply allowance/cost; full army-list legality is outside initial scope.                              |
| Campaign points                      | Configure, Calculate, Display | Shown while a campaign is in progress or completed, not while upcoming. Show a running per-player table of current structure holdings, cumulative battle campaign points, awarded, ranking, and running public objectives, and visible held item-objective points; omit hidden item sources from unauthorized totals. Ranking objectives and points per territory show a top five; 0-point objectives are ignored. Allied relic control is not shown as a top five. |
| Longest connected territory chain    | Calculate                     | Calculate from player-owned territory graph; allied territories excluded. Friendly first-place ties each receive the configured campaign points. |
| Structure-control objective          | Calculate                     | Rank by currently owned non-destroyed structure campaign points. Friendly first-place ties each receive the configured campaign points. |
| Points per territory                 | Calculate, Display            | When configured above 0, each currently owned territory awards that many public-objective campaign points. Show a top five by current territory count. |
| Allied relic control                 | Calculate                     | When configured above 0, each revealed relic held by another player of the same faction or a current ally awards that many public-objective campaign points. |
| Public objectives                    | Configure, Calculate, Display | Track progress and show criteria/results.                                                                                      |
| Private objectives                   | Configure, Secret, Calculate, Audit | Catalog at setup; assign to players, factions, and ally groups. Manual claims need manager approval. Automatic criteria score from map facts and become public when completed. Unclaimed counts are public; text stays secret until reveal or campaign completion. |
| Backstabber objective                | Calculate, Secret             | Remove alliance-objective eligibility and assign configured replacement.                                                       |
| Hidden item-objective placement      | Secret, Configure             | Random or manager-placed; skip spawn unless allowed; never include unrevealed location in player responses.                    |
| Item-objective discovery/transfer    | Calculate, Audit              | Reveal when found or staff-revealed in debug; drop on move/retreat; pickup by a lone force; battle winner takes spoils.        |
| Relic choices/effects                | Configure, Secret, Calculate, Audit | Configure flavor text and named choices in setup. Resolving a choice applies one result or a random result from that choice's group: new flavor text, optional new state label, optional secret private objective, and optional destroy-and-replace. Destroyed items award no points and leave the map. |
| Force statuses                       | Calculate, Display            | Track one named status per force (Normal is none). Setup configures enable/clear triggers; the standard catalog is Diseased, Shaken, Confident, Exhausted, and Well Rested. Effect text is display-only. |
| Faction choice                       | Configure, Enforce            | Players pick a faction (and required subfaction) before they can play; they may change it until the campaign starts, then it is locked. Managers and administrators may assign another player's faction after launch for fixes and testing. |
| Faction alignments                   | Configure                     | Model alliance groups and unaligned factions per campaign. Renaming a group keeps assigned factions in that group. |
| Faction map rules                    | Configure, Enforce/Calculate  | Structured modifiers may affect movement, supply, spawn, relic sensing, status, pillage, or retreat.                           |
| Faction tabletop rules               | Configure, Display            | Reusable special-rule catalog (name, description, optional effect key) assigned to factions, subfactions, and item objectives. Hunt in Estalia copies named faction-sheet wording. User-created rules are display-only and do not execute code. |
| Rule/version changes                 | Configure, Audit              | Record source/version/effective round; activate approved changes at a round boundary.                                          |
| GM order inspection                  | Secret, Audit                 | Permit inspection only inside a logged debug session; unrevealed orders stay out of the public log. Notify affected players when debug ends. |
| GM corrections                       | Enforce, Audit                | Enter debug (logged), append staff corrections without overwriting originals, re-resolve the last window only while the following phase is open (or post-campaign grace), uncommit or nullify only directly affected current-phase orders, then exit debug (logged, notifying players). |
| Multiple GMs                         | Enforce, Audit                | Support concurrent campaign memberships and optimistic concurrency.                                                            |
| Neutral intervention                 | Configure, Audit              | Ephemeral GM ringer battle against an idle player force in an open battle phase; ringer leaves no map trace. Drought occupation is out of scope. |
| Delinquency                          | Enforce, Audit, Notification  | Per-force campaign-lifetime count of missing drafts, missing required retreats, and neither-side missed results. Notify managers from the third offence as a possible kick; never auto-remove. |

## Faction-rule implementation categories

The supplied faction rules demonstrate why modifiers need explicit categories rather than
arbitrary scripts.

### Map-enforced or calculated

- Extended movement with intermediate-territory interception.
- Extra structure supply.
- Pillage strength and allowed targets.
- Status immunity/transfer.
- Retreat destination exceptions.
- Hidden-relic proximity hint and relic-directed movement.
- Terrain-as-structure supply/defense.
- Alternative supply generation.
- Alternative/random spawn behavior.
- Forced movement toward a revealed relic.
- Extra temporary supply from pillage.
- Extra Black Powder spend declared on the battle result.
- Magical Supply leftover rerolls declared on the battle result (capped at unused composition supply).

### Display or battle metadata

- Ambush modifiers.
- One-use battle abilities.
- Terrain-specific tabletop modifiers.
- Army unit eligibility and mercenary effects.

The first group may use tested, typed policy implementations. The second group is reference
content or structured mission metadata. Do not execute administrator-provided code.

## Unfinished or ambiguous draft areas

These remain in `DECISIONS-NEEDED.md` and must not be silently resolved by an agent:

- Castle siege mechanics (gates, walls, battering rams, scaling, and inside-the-walls) are out of
  application scope. Castle remains a structure type; tabletop castle features stay display-only.
- Armies-of-infamy content is out of application scope. Those lists are ordinary subfaction
  configuration and may be added later as preset or catalog data.
- Relic choice options, result groups, destroy-and-replace, and granted secret objectives are configured in setup. Named Hunt special-rule effect keys are enforced or calculated as documented in DOMAIN.md; remaining tabletop dice effects stay display-only.
- Exact proprietary battle-point conversion charts are out of scope; managers enter already-converted
  scores or raw victory points and configure multiplier, clamp, and negative-loser behavior.
