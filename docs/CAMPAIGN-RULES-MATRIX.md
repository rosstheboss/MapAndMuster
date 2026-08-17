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
| Campaign play log                    | Audit, Display                | Show a log on the campaign page (upcoming through completed) with timestamps and originators; record campaign start, schedule extensions, revealed actions including Hold, invalid or conflicting submitted actions that became Hold, battles, manager battle-result overrides, retreats, and automatic force rejoins; accept member chat to public, direct, faction, or ally-group channels with `@` tags of current members only; let composers type and autocomplete Everyone or a member username; link chat originators and mentions to public profiles; omit private chats from unauthorized payloads, including campaign managers; never log unrevealed orders. |
| Participants and public profiles     | Display, Secret               | List attached members with faction and Manager/Player/Admin roles; open public profiles from member names; list only campaigns the viewer may already view. |
| Notifications and home board         | Notification, Display         | Notify in-app and/or by email of mentions, private chats, campaign start, campaign end, new phases, and actions that still need the user; show those items on Home, or "No new notifications." when none remain; email bodies omit hidden orders and private chat text. |
| Site news                            | Display                       | Show administrator-authored markdown news on Home, one article per page. |
| Missing order                        | Enforce                       | Create Hold for every missing required force/action slot.                                                                      |
| Move                                 | Enforce, Calculate            | Validate controller, origin, destination, adjacency, spawn restrictions, and faction modifiers. Invalid Move becomes Hold.     |
| Hold                                 | Enforce, Calculate            | Preserve location and trigger configured rest/status effects.                                                                  |
| Build                                | Enforce, Calculate            | Validate structure slot and a buildable type; create owner/controller state.                                                   |
| Pillage                              | Enforce, Calculate            | Validate relationship, pillageable/destructible flags, and battle precedence; progress or remove the structure.                |
| Repair                               | Enforce, Calculate            | Validate ownership/control rules and restore eligible structure.                                                               |
| Split/rejoin forces                  | Enforce, Calculate, Audit     | Enforce force limit and target eligibility; independently track split forces; automatically rejoin co-located same-player forces into one action and log the rejoin. |
| Backstab                             | Enforce, Calculate, Audit     | Validate context, permanently change applicable alliance relationships, pillage where required, and create battles.            |
| Retreat                              | Enforce, Calculate            | Validate eligible destinations, resolve collisions, and use spawn fallback.                                                    |
| Automatic Battle                     | Calculate                     | Create engagement when enemy forces remain together after precedence resolution.                                               |
| Surrender                            | Enforce, Calculate            | Record decisive result, territory consequence, and required retreat without secondary rewards.                                 |
| Multiple combatants                  | Configure, Calculate          | Assign a compatible multi-party mission/result schema; do not assume two-party scoring.                                        |
| Allied co-location                   | Calculate                     | Determine territory claimant using configured ranking/tie-break rules.                                                         |
| Map image and territories            | Configure                     | Upload raster, draw/import polygons, assign IDs, terrain, ownership, spawn, structures, structure condition, and adjacency. A pair of territories has at most one connection. |
| Spawn behavior                       | Enforce, Calculate            | Prohibit enemy entry/battle/build, provide base supply, combine same-player split forces, and drop carried item objectives on the territory a force leaves. |
| Terrain types                        | Configure, Display            | Select mission/layout candidates and show tabletop rules. Standard terrain is a replaceable preset copied from the current catalog. |
| Tabletop terrain features            | Display                       | Present woods, water, river, bridge, cliff, castle, and similar rules with the mission; do not simulate tabletop movement.     |
| Supply network                       | Calculate                     | Graph traversal through owned/allied adjacent territories; calculate per-force normal supply.                                  |
| Temporary supply                     | Calculate, Audit              | Award, retain, allocate, and consume only for an eligible played battle.                                                       |
| Army escalation                      | Configure, Calculate          | Configure per-round points, free supply, and free-character allowances.                                                        |
| Army composition                     | Calculate, Display            | Calculate campaign-level supply allowance/cost; full army-list legality is outside initial scope.                              |
| Army-list submission                 | Configure, Secret             | Store file/link and declared temporary supply; expose to participant, opponent, and staff as configured.                       |
| Campaign points                      | Calculate, Audit              | Maintain current and historical components; expose public totals while concealing secret sources.                              |
| Longest connected territory chain    | Calculate                     | Calculate from player-owned territory graph; allied territories excluded in supplied rules.                                    |
| Structure-control objective          | Calculate                     | Calculate qualified structures and configured tie behavior.                                                                    |
| Public objectives                    | Configure, Calculate, Display | Track progress and show criteria/results.                                                                                      |
| Private objectives                   | Configure, Secret, Calculate  | Assign at player/faction/alliance scope; conceal progress and award until reveal policy permits.                               |
| Backstabber objective                | Calculate, Secret             | Remove alliance-objective eligibility and assign configured replacement.                                                       |
| Hidden item-objective placement      | Secret, Configure             | Random or manager-placed; skip spawn unless allowed; never include unrevealed location in player responses.                    |
| Item-objective discovery/transfer    | Calculate, Audit              | Reveal when found or staff-revealed in debug; drop on move/retreat; pickup by a lone force; battle winner takes spoils.        |
| Relic choices/effects                | Configure, Secret, TBD        | Provide a versioned choice/effect mechanism after rules are supplied. Placement, visibility, and transfer are implemented.     |
| Force statuses                       | Calculate, Display            | Track one status per force; apply transition/recovery rules and show tabletop effects.                                         |
| Faction choice                       | Configure, Enforce            | Players pick a faction (and required subfaction) before they can play; they may change it until the campaign starts, then it is locked. |
| Faction alignments                   | Configure                     | Model alliance groups and unaligned factions per campaign.                                                                     |
| Faction map rules                    | Configure, Enforce/Calculate  | Structured modifiers may affect movement, supply, spawn, relic sensing, status, pillage, or retreat.                           |
| Faction tabletop rules               | Configure, Display            | Show assigned rules with mission/battle; do not simulate tabletop dice/unit effects.                                           |
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
- Relic choices and state-changing effects are intentionally unrevealed.
- Several armies-of-infamy entries are placeholders.
- Exact battle-point conversion is delegated to another guide.
- Several scoring rows do not fully define whether points are current, cumulative, or both.
- Tie handling is not defined for every objective.
- Some retreat, inactivity, no-result, and campaign-removal cases need confirmation.
