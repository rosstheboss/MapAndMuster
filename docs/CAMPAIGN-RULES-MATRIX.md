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
| Campaign length and round schedule   | Configure, Enforce, Audit     | Configure rounds/deadlines; after launch allow round-count increase and lengthening remaining windows, not shortening them.    |
| Two action windows and battle phase  | Enforce                       | Maintain explicit timed states; Action 1 battle can force later Battle action.                                                 |
| Draft, commit, uncommit, early close | Enforce, Audit                | A confirmed map action or force-panel checkmark saves a draft. Commit is allowed only after every required force has a saved draft. The latest draft commits at the deadline or on commit. Uncommit back to draft only while the action window is open. Close atomically. Unused battle time flows into the next window. |
| Campaign play log                    | Audit, Display                | Show a log on the campaign page (upcoming through completed) with timestamps and originators; record campaign start, schedule extensions, revealed actions including Hold, invalid or conflicting submitted actions that became Hold, battles, manager battle-result overrides, retreats, and automatic force rejoins; accept member chat to public, direct, faction, or ally-group channels with `@` tags of current members only; let composers type and autocomplete Everyone or a member username; link chat originators and mentions to public profiles; omit private chats from unauthorized payloads, including campaign managers; never log unrevealed orders or site-wide chat. |
| Site chat                            | Display, Notification         | Show a public site-wide chat on All Campaigns, separate from campaign logs; allow `@` tags of any account; reject prohibited language; let users block one another so player messages hide both ways; let administrators announce to everyone or one person with notifications; flag and filter languages without translating. |
| Participants and public profiles     | Display, Secret               | List attached members with faction and Manager/Player/Admin roles; open public profiles from member names; list only campaigns the viewer may already view. |
| Notifications and home board         | Notification, Display         | Notify in-app and/or by email of mentions, private chats, campaign start, campaign end, new phases, actions that still need the user, site-chat mentions, and administrator site-chat announcements; show those items on Home, or "No new notifications." when none remain; email bodies omit hidden orders, private chat text, and site-chat bodies. |
| Site news                            | Display                       | Show administrator-authored markdown news on Home, one article per page. |
| Missing order                        | Enforce                       | Create Hold for every missing required force/action slot.                                                                      |
| Move                                 | Enforce, Calculate            | Validate controller, origin, destination, adjacency, spawn restrictions, and faction modifiers. Invalid Move becomes Hold.     |
| Hold                                 | Enforce, Calculate            | Preserve location and trigger configured rest/status effects.                                                                  |
| Build                                | Enforce, Calculate            | Validate structure slot and a buildable type; create owner/controller state.                                                   |
| Pillage                              | Enforce, Calculate            | Validate relationship, pillageable/destructible flags, and battle precedence; progress or remove the structure.                |
| Repair                               | Enforce, Calculate            | Validate ownership/control rules and restore eligible structure.                                                               |
| Split/rejoin forces                  | Enforce, Calculate, Audit     | Enforce force limit and target eligibility; independently track split forces; automatically rejoin co-located same-player forces into one action and log the rejoin. |
| Backstab                             | Enforce, Calculate, Audit     | Validate context, permanently change applicable alliance relationships, pillage where required, and create battles.            |
| Retreat                              | Enforce, Calculate, Audit     | Validate eligible destinations, resolve collisions by strongest (campaign points, territories, structures, supply including temporary, then recorded random), and use spawn fallback. |
| Automatic Battle                     | Calculate                     | Create engagement when enemy forces remain together after precedence resolution.                                               |
| Surrender                            | Enforce, Calculate, Audit     | Allowed while engaged in an action or battle window. Committed surrender cannot be withdrawn; a draft still executes at the deadline. 1v1 awards max VP battle points with no extra BP. Remaining side wins if everyone else runs; mutual run is a no-contest. |
| Multiple combatants                  | Calculate                     | Allied extras add 25 percent round army points per extra player, split evenly, round up to 10. More than two opposing sides: two strongest play first, then strongest-to-weakest in the same phase; unplayed forces stay for the next battle phase. |
| Allied co-location                   | Calculate                     | Determine territory claimant using configured ranking/tie-break rules.                                                         |
| Map image and territories            | Configure                     | Upload raster, draw/import polygons, assign IDs, terrain, ownership, spawn, structures, structure condition, and adjacency. A pair of territories has at most one connection. |
| Spawn behavior                       | Enforce, Calculate            | Prohibit enemy entry/battle/build, provide base supply, combine same-player split forces, and drop carried item objectives on the territory a force leaves. |
| Terrain types                        | Configure, Display            | Select mission/layout candidates and show tabletop rules. Standard terrain is a replaceable preset copied from the current catalog. Each type has a Water feature flag for special-rule interaction and player reminders; it does not change movement by itself. |
| Tabletop terrain features            | Display                       | Present woods, water, river, bridge, cliff, castle, and similar rules with the mission; do not simulate tabletop movement.     |
| Supply network                       | Configure, Calculate, Display | Graph traversal from spawn through owned or allied (same group, not backstabbed) territories. Terrain and operational owned structures grant configured supply points (default 1). Show current supply on the Participants list and on battles to resolve. |
| Temporary supply                     | Configure, Calculate, Audit   | Award configured pillage/destroy points (default 1) to the earning player. That player may spend the pool on any of their forces; each point applies to exactly one force. Battle reports spend supply-costing units from force allowance first, then this pool. |
| Split-force supply                   | Configure, Calculate          | Each split force claims the same territory/structure supply minus the split penalty (Hunt default 25 percent), minimum 1 each, plus the full round free-supply bonus. |
| Army escalation                      | Configure, Calculate          | Configure per-round max army points, free supply, and free characters. Hunt in Estalia values are the application default. |
| Battle result reporting              | Configure, Enforce, Audit     | Players report both sides (VP, army points, supply-costing unit count, differential BP, bonus BP, optional general kill / supply-line destruction, mission questions). Agree, dispute, staff confirm, or deadline-commit. True BP ties force both to retreat without a loss. |
| Campaign presets                     | Configure                     | Built-in Hunt in Estalia catalog plus administrator-saved named presets that include map data. |
| Army composition                     | Calculate, Display            | Calculate campaign-level supply allowance/cost; full army-list legality is outside initial scope.                              |
| Army-list submission                 | Configure, Secret             | Store file/link and declared temporary supply; expose to participant, opponent, and staff as configured.                       |
| Campaign points                      | Configure, Calculate, Display | Show a running per-player table of current structure holdings, cumulative battle campaign points, awarded and ranking public objectives, and visible held item-objective points; omit hidden item sources from unauthorized totals. Ranking objectives show a top five; 0-point objectives are ignored. |
| Longest connected territory chain    | Calculate                     | Calculate from player-owned territory graph; allied territories excluded. Friendly first-place ties each receive the configured campaign points. |
| Structure-control objective          | Calculate                     | Calculate qualified structures and configured tie behavior.                                                                    |
| Public objectives                    | Configure, Calculate, Display | Track progress and show criteria/results.                                                                                      |
| Private objectives                   | Configure, Secret, Calculate, Audit | Catalog at setup; assign to players, factions, and ally groups. Manual claims need manager approval. Automatic criteria score from map facts and become public when completed. Unclaimed counts are public; text stays secret until reveal or campaign completion. |
| Backstabber objective                | Calculate, Secret             | Remove alliance-objective eligibility and assign configured replacement.                                                       |
| Hidden item-objective placement      | Secret, Configure             | Random or manager-placed; skip spawn unless allowed; never include unrevealed location in player responses.                    |
| Item-objective discovery/transfer    | Calculate, Audit              | Reveal when found or staff-revealed in debug; drop on move/retreat; pickup by a lone force; battle winner takes spoils.        |
| Relic choices/effects                | Configure, Secret, Calculate, Audit | Configure flavor text and named choices in setup. Resolving a choice applies one result or a random result from that choice's group: new flavor text, optional new state label, optional secret private objective, and optional destroy-and-replace. Destroyed items award no points and leave the map. |
| Force statuses                       | Calculate, Display            | Track one named status per force (Normal is none). Setup configures enable/clear triggers; the standard catalog is Diseased, Shaken, Confident, Exhausted, and Well Rested. Effect text is display-only. |
| Faction choice                       | Configure, Enforce            | Players pick a faction (and required subfaction) before they can play; they may change it until the campaign starts, then it is locked. Managers and administrators may assign another player's faction after launch for fixes and testing. |
| Faction alignments                   | Configure                     | Model alliance groups and unaligned factions per campaign.                                                                     |
| Faction map rules                    | Configure, Enforce/Calculate  | Structured modifiers may affect movement, supply, spawn, relic sensing, status, pillage, or retreat.                           |
| Faction tabletop rules               | Configure, Display            | Reusable special-rule catalog (name and description) assigned to factions and item objectives the same way missions are reused. Pre-configured rules copy a generic catalog description (no faction-specific wording or flavor). User-created rules are display-only and do not execute code or change resolution. |
| Rule/version changes                 | Configure, Audit              | Record source/version/effective round; activate approved changes at a round boundary.                                          |
| GM order inspection                  | Secret, Audit                 | Permit inspection only inside a logged debug session; unrevealed orders stay out of the public log. Notify affected players when debug ends. |
| GM corrections                       | Enforce, Audit                | Enter debug (logged), append staff corrections without overwriting originals, re-resolve the last action only while the following phase is open, then exit debug (logged, notifying players). |
| Multiple GMs                         | Enforce, Audit                | Support concurrent campaign memberships and optimistic concurrency.                                                            |
| Neutral intervention                 | Configure, Audit              | GM controls a neutral force; record purpose and scoring/ownership permissions.                                                 |

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

### Display or battle metadata

- Ambush modifiers.
- Casting/dispelling resources.
- One-use battle abilities.
- Terrain-specific tabletop modifiers.
- Army unit eligibility and mercenary effects.

The first group may use tested, typed policy implementations. The second group is reference
content or structured mission metadata. Do not execute administrator-provided code.

## Unfinished or ambiguous draft areas

These remain in `DECISIONS-NEEDED.md` and must not be silently resolved by an agent:

- Castle gates, walls, battering rams, scaling, and inside-the-walls rules are headings only.
- Relic choice options, result groups, destroy-and-replace, and granted secret objectives are configured in setup. Mechanical map modifiers from special rules remain display-only until specified.
- Several armies-of-infamy entries are placeholders.
- Exact proprietary battle-point conversion charts are out of scope; managers enter already-converted
  scores or raw victory points and configure multiplier, clamp, and negative-loser behavior.
- Several scoring rows do not fully define whether points are current, cumulative, or both.
- Some retreat, inactivity, no-result, and campaign-removal cases need confirmation.
