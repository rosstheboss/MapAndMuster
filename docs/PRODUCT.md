# Product Definition

## Purpose

The product is **Map & Muster**, a generic web application for running simultaneous-order map campaigns. It
maintains authoritative map and campaign state, privately accepts player decisions, reveals
and resolves actions, records tabletop battle results, calculates campaign resources and
points, and gives authorized staff auditable correction tools.

The initial target is approximately 8-20 participants in a campaign lasting about 8-10 rounds,
with the ability to extend the campaign.

## Users

- **Player:** controls one or more forces, submits orders and battle results, views authorized
  campaign information, and receives public or private objectives.
- **Game Master:** manages a campaign, may also participate as a player, may inject ephemeral
  ringer battles, inspects/corrects state with mandatory auditing and notifications, and resolves
  disputes.
- **Administrator:** manages the application and may perform GM or player capabilities.

Roles are campaign-scoped except for the system-wide Administrator role. A user may be both
Player and Game Master in the same campaign.

## Core capabilities

- Account registration with unique username, name (including optional suffix), location, display
  time zone, optional avatar, email verification, password reset, signed-in password change, and
  optional Google/Facebook/Discord sign-in. The profile stores a date-and-time display format
  (Month Day, Year, Time Timezone with seconds by default).
- Campaign creation, membership, factions, alliances, forces, rounds, deadlines, and roles.
- Raster-map upload with polygon territories, adjacency, terrain, structures, spawn locations,
  ownership, force/relic markers, and viewer-selected map highlight colors.
- Campaign-point standings with sortable current-holdings totals, ranking and points-per-territory
  public-objective leaderboards, named public-objective awards, revealed private-objective totals, unclaimed
  private-objective counts, and relic logos.
- Secret draft/commit/uncommit order entry with deadline and per-phase early-close behavior.
- Simultaneous action reveal and deterministic, explainable resolution.
- Battle creation, mission assignment from structure then terrain catalogs (attacker/defender
  roles when the engagement calls for them), dual result submissions (including early reports during
  Action windows), agreement or dispute, no-result forced retreats, staff confirmation, retreats,
  scoring, and optional GM ringer battles against idle forces.
- Administrators can save campaign settings and map data as named reusable presets, and can download
  or upload a portable preset package (catalog, overlay, map image, and uploaded logos) between hosts.
- Campaign-point, supply-line, temporary-supply, status, objective, and relic tracking.
- Configurable force statuses with enable/clear triggers and display-only tabletop effects.
- Public faction rules and private player/faction/alliance objectives.
- Multiple GMs, ephemeral ringer battles, campaign extension, corrections, revision history, and audit.
- Manager add/kick of players, promotion of a player to campaign manager, bringing in a user as
  manager-only or as manager and player, delinquency kick recommendations from the third missed-order
  offence, and staff assignment of another player's faction.
- Ending a campaign (closing play while keeping the final state for logs and duplication).
- In-app and email notifications.
- Public site-wide chat on All Campaigns, with language flags, block lists, and administrator announcements.
- Seeded administrator test accounts (Test 1–Test 45) that skip email and public site chat.

## Product boundaries

The application does not simulate tabletop battles or validate complete tabletop army lists in
the initial scope. It may store submitted list text and calculate campaign-level composition
allowances. For Warhammer: The Old World, pasted New Recruit or Old World Builder text may be
parsed to suggest supply amounts; the player confirms or corrects those numbers. Tabletop-only
terrain and faction rules are displayed with assigned missions but are not mechanically
simulated unless later specified.

Bundled content remains generic. Administrators provide their own campaign text, maps,
missions, factions, rules, and imagery at runtime.

## Success criteria for the first campaign

- Twenty users can submit private orders without leaking them to other players.
- The last required commitment or deadline closes a planning window exactly once.
- All resolutions are deterministic or explicitly routed to a GM.
- A GM can explain and audit every automatic or manual state change.
- Map, supply, scoring, objective, battle, and relic state remain consistent across rounds.
- Critical flows work on current desktop and mobile browsers.
