# Decisions Needed

Agents must not choose answers to these items without user direction. Record resolved decisions
in an ADR or the relevant domain document and update tests.

## Campaign and phase behavior

1. Resolved: a battle phase ends early when every engagement is finalized and every required
   retreat is recorded. Unused time is added to the next action or battle window. See
   `docs/DOMAIN.md`.
2. Resolved: if neither side submits a result, the engagement is a no-contest. Every force that
   fought that tabletop game, including silent allies, is forced to retreat after all other
   retreats in the phase, then spawn if needed. No win, draw, battle campaign points, or spoils;
   skip Shaken. Waiting other sides stay. See `docs/DOMAIN.md`.
3. Resolved: removal is not automatic. Offences are counted per force for the whole campaign.
   Managers are notified from the third offence onward as a possible kick. See `docs/DOMAIN.md`.
4. Resolved: only the immediately previous window can be corrected, and only while the following
   phase is still open (or during a post-campaign grace). Re-resolve that window; uncommit or
   nullify only directly affected current-phase orders. See `docs/DOMAIN.md`.

## Resolution details

5. Resolved: simultaneous action processing order is movement and splits, then backstab alliance
   breaks, then battles from enemy co-location, then Build/Pillage/Repair for forces not in
   battle. Player-facing action order is Hold, Move, Build, Pillage, Repair, Split, Backstab.
   Competing Build, Pillage, or Repair on the same territory, and other collisions that still
   lack a documented ranking, become Hold. Retreat is battle-phase only. See `docs/DOMAIN.md`.
6. Resolved: in a retreat collision the strongest force keeps the territory. Strongest is most
    current campaign points, then most territories, then most structures, then most supply
    (including remaining temporary supply). Remaining ties are chosen at random and recorded.
    See `docs/DOMAIN.md`.
7. Resolved: allied extra players on one side raise that side's round army-point cap by 25
    percent per extra player, then split the total evenly and round each force up to the next
    10. More than two opposing sides who do not retreat: the two strongest play first, then
    remaining opponents strongest-to-weakest in the same battle phase. A force that never
    played stays in the territory for the next round's battle phase. See `docs/DOMAIN.md`.
8. Resolved: surrender may be submitted during an action or battle window while the force is
    engaged. A committed surrender cannot be withdrawn. A surrender left in draft still
    executes at the deadline. In 1v1 the remaining player wins at maximum victory-point battle
    points with no extra/mission bonus battle points. In larger fights, allies of a surrendering
    force may keep fighting or run; if only one side remains it wins; if every remaining force
    runs, nobody wins and no relic transfers. See `docs/DOMAIN.md`.
9. Resolved: the territory owner owns the structure. Enemy capture stays operational unless a
   special rule auto-pillages. Allies cannot claim allied land without backstabbing. Empty-land
   backstab claims and auto-pillages (never auto-destroys). See `docs/DOMAIN.md`.

## Scoring and supply

10. Resolved: territory control does not award campaign points by itself unless a running
    public objective for points per territory is configured above 0. Most territories
    currently controlled remains a ranking public objective. See `docs/DOMAIN.md`.
11. Resolved: structure campaign points are a running total of current holdings, recalculated
    from live map state. Destroyed structures do not count; pillaged structures still count.
    Ranking public objectives use friendly ties: every player currently tied for first after
    documented tie-breaks receives the points. Most battles won ranks by wins, then draws.
    See `docs/DOMAIN.md`.
12. Resolved: battle campaign points default to differential scoring. The winner receives the
    clamped (winner score minus loser score) times a multiplier (default 1, never 0), default
    range 0 to 10. Draw participants each receive configured draw points (default 1). Straight
    win points (default 2) apply only when differential scoring is off. Negative points for the
    loser are off by default. Scores are reported tabletop or already-converted battle points;
    the application does not copy a proprietary conversion chart. Wins and draws are still
    recorded for ranking. See `docs/DOMAIN.md`.
13. Resolved: temporary supply belongs to the earning player. That player may spend it on any
    of their forces. Each spent point applies to exactly one force; split forces cannot share a
    single point. See `docs/DOMAIN.md`.

## Content and unfinished rules

14. Resolved: castle siege mechanics (gates, walls, battering rams, scaling, and
    inside-the-walls behavior) are out of application scope. Castle remains an existing
    structure type. See `docs/DOMAIN.md`.
15. Resolved: item-objective flavor text, holder choices, and choice results (including
    destroy-and-replace) are configured in campaign setup. Private objectives are a setup
    catalog assigned to players, factions, and ally groups. See `docs/DOMAIN.md`. Placement
    (random or manager-placed), hidden-until-found visibility, spawn eligibility, drop-on-move,
    pickup, battle transfer, and staff reveal in debug mode remain as previously implemented.
16. Resolved: armies-of-infamy content is out of application scope. Those lists are ordinary
    subfaction configuration, not a dedicated feature. See `docs/DOMAIN.md`.
17. Resolved: a Neutral territory is unowned land. Neutral forces are ephemeral GM ringer
    battles, not persistent armies. Drought occupation is not applicable. See `docs/DOMAIN.md`.

## Operations

18. Partially resolved: first production hosting is Render, DNS is Cloudflare, transactional email
    is Resend, and background work stays in the API process. Object storage is still local disk.
    See `docs/adr/0003-production-hosting-stack.md`.
19. Decide registration anti-abuse policy and campaign invitation/join-code workflow.
20. Resolved: keep custom dialogs and confirm buttons; do not add a CSS framework, component
    library, or client state library. Angular signals remain the UI state approach. See
    `docs/UI-AUDIT-2026-08.md` step 2 (`UI-C3`, `UI-C4`, `UI-M4`), implemented 2026-08-30.

## Interface

21. Resolved: any signed-in player may create a campaign. "Your campaigns" empty state should
    include a Join campaign shortcut to All campaigns. Remaining setup choices on a joined
    campaign (such as selecting a faction) show on the campaign card and as persisted backend
    notifications on Home. See `docs/UI-AUDIT-2026-08.md` (`UI-M6`, `UI-M7`). Campaign list
    items now expose `canChooseFaction`, `isCommitted`, and `currentPhaseKind`. Home already
    derives "Choose your faction" from membership. Card and Home dashboard UI still follow in
    the entry-points step; do not fake remaining setup only in the client.
22. Resolved: the map is not editable after a campaign starts. See `UI-H6`.
23. Resolved: during a running campaign, Actions, Chat, and Standings are expanded by default;
    afterwards persist that player's last open/closed set per campaign. Page order is Actions,
    then Chat log, then Map. Unread mentions and private messages show unread indicators on the
    chat log, with last-read state persisted on the server so they follow the player across
    devices. See `UI-H2`. `GET /api/campaigns/{id}/log` now returns `lastReadUtc`,
    `unreadMentionCount`, and `unreadPrivateCount`. `POST /api/campaigns/{id}/log/read`
    records last-read from the server clock and does not bump campaign revision. Per-player
    open/closed campaign-page sections still need an API contract in the campaign-page step.
24. Resolved: managers and administrators end a campaign rather than deleting it. The campaign
    stays stored in its final state (closed / Completed), remaining orders are not resolved,
    members can still open logs and duplicate it, and all current members are notified in-app
    and by email. End campaign is available on the campaign page and Edit campaign. Staff may
    promote a player to campaign manager or add a user as manager-only or as manager and
    player. See `docs/DOMAIN.md`.
