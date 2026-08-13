# Decisions Needed

Agents must not choose answers to these items without user direction. Record resolved decisions
in an ADR or the relevant domain document and update tests.

## Campaign and phase behavior

1. May a battle phase end early when all engagements and retreats are finalized, or does the
   configured deadline always remain authoritative?
2. If neither battle participant submits a result, is the battle disputed, a forced retreat for
   both, a GM task, or another outcome?
3. The draft mentions removal after three missing-action/forced-retreat incidents. Is removal
   automatic, a GM recommendation, or configurable? What exactly increments the count?
4. When a prior-round correction changes downstream state, which later actions/results are
   automatically invalidated versus flagged for GM/player review?

## Resolution details

5. Define full simultaneous-action precedence, including Move, Split, Build, Pillage, Repair,
   Backstab, Retreat, and multiple arrivals.
6. In a retreat collision, define “strongest” and every tie-break step.
7. Define territory/control outcomes for three or more combatants and unresolved battles.
8. Define surrender timing and whether a submitted surrender can be withdrawn.
9. Clarify structure ownership versus territory control after repair, capture, backstab, retreat,
   and allied use.

## Scoring and supply

10. Is “Territory claimed” a cumulative one-time award, current territory value, or both?
11. Are ordinary structure/territory campaign points recalculated each round or only at campaign
    end? How are ties handled for most structures/longest chain?
12. Define the Battle Point Difference to Campaign Point conversion and permitted negative
    values/caps.
13. Confirm whether temporary supply belongs to player, faction, or earning force, and define
    allocation when multiple forces battle in one round.

## Content and unfinished rules

14. Define castle gates, walls, battering rams, scaling, and inside-the-walls behavior.
15. Define relic choice options, private/public timing, effects, and recovery when a choice
    changes campaign state.
16. Complete armies-of-infamy content or mark it explicitly out of application scope.
17. Define neutral-force eligibility, drought threshold, scoring, territory capture, relic/objective
    interaction, and whether a participating GM may control it against their own rivals.

## Operations

18. Select production hosting, object storage, email provider, and background-job mechanism.
19. Decide registration anti-abuse policy and campaign invitation/join-code workflow.
20. Select a UI component library, if any, after an initial custom-CSS prototype.
