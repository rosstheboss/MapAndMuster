# Decisions Needed

Agents must not choose answers to these items without user direction. Record resolved decisions
in an ADR or the relevant domain document and update tests.

## Campaign and phase behavior

1. Resolved: a battle phase ends early when every engagement is finalized and every required
   retreat is recorded. Unused time is added to the next action or battle window. See
   `docs/DOMAIN.md`.
2. If neither battle participant submits a result, is the battle disputed, a forced retreat for
   both, a GM task, or another outcome?
3. The draft mentions removal after three missing-action/forced-retreat incidents. Is removal
   automatic, a GM recommendation, or configurable? What exactly increments the count?
4. When a prior-round correction changes downstream state, which later actions/results are
   automatically invalidated versus flagged for GM/player review?

## Resolution details

5. Resolved: simultaneous action processing order is movement and splits, then backstab alliance
   breaks, then battles from enemy co-location, then Build/Pillage/Repair for forces not in
   battle. Player-facing action order is Hold, Move, Build, Pillage, Repair, Split, Backstab.
   Competing Build, Pillage, or Repair on the same territory, and other collisions that still
   lack a documented ranking, become Hold. Retreat is battle-phase only. See `docs/DOMAIN.md`.
6. In a retreat collision, define “strongest” and every tie-break step.
7. Define territory/control outcomes for three or more combatants and unresolved battles.
8. Define surrender timing and whether a submitted surrender can be withdrawn.
9. Clarify structure ownership versus territory control after repair, capture, backstab, retreat,
   and allied use.

## Scoring and supply

10. Resolved: territory control does not award campaign points by itself. Most territories
    currently controlled is a ranking public objective. See `docs/DOMAIN.md`.
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
13. Confirm whether temporary supply belongs to player, faction, or earning force, and define
    allocation when multiple forces battle in one round.

## Content and unfinished rules

14. Define castle gates, walls, battering rams, scaling, and inside-the-walls behavior.
15. Define relic choice options, private/public timing, effects, and recovery when a choice
    changes campaign state. Placement (random or manager-placed), hidden-until-found
    visibility, spawn eligibility, drop-on-move, pickup, battle transfer, and staff reveal
    in debug mode are implemented.
16. Complete armies-of-infamy content or mark it explicitly out of application scope.
17. Define neutral-force eligibility, drought threshold, scoring, territory capture, relic/objective
    interaction, and whether a participating GM may control it against their own rivals.

## Operations

18. Select production hosting, object storage, email provider, and background-job mechanism.
19. Decide registration anti-abuse policy and campaign invitation/join-code workflow.
20. Select a UI component library, if any, after an initial custom-CSS prototype.
