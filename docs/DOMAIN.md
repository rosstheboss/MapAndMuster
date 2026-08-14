# Domain Model and Invariants

## Core language

- **Campaign:** one configured map campaign with members, rules, rounds, and content.
- **Round:** one complete action and battle cycle. More rounds may be appended.
- **Action Window:** a timed simultaneous-order window, normally Action 1 or Action 2.
- **Order Draft:** the latest saved, valid player intent for a force and action slot.
- **Commitment:** a player's declaration that all currently required orders are ready.
- **Force:** a movable campaign formation controlled by a player or neutral GM participant.
- **Territory:** a polygonal map region with explicit adjacency, terrain, ownership, and
  optional structure/spawn metadata.
- **Battle:** an engagement created by resolved campaign actions.
- **Result Submission:** one participant's structured report for a battle.
- **Campaign Revision:** monotonically increasing version of authoritative campaign state.

## Accounts and public identity

An account has a unique username (3-32 characters, starting with a letter, then letters,
digits, or underscores; English profanity, racial slurs, and similar abusive terms are
rejected), email, password or external login, legal name, optional middle initial, optional
name suffix (Jr., Sr., or Roman numerals I-X), city/region/country, IANA time-zone
preference, optional avatar, and a display-name preference. First and last names are at least
two characters and use the same prohibited-language rule as usernames. Local passwords must
be at least 12 characters and include uppercase, lowercase, a number, and a special character.
Created and last-edited instants are stored in UTC. The owner chooses a time zone for display;
when none has been stored yet, those times are shown in UTC.

Other users may see username, location, avatar, and either the username or the full name
according to that preference. Email, created/updated timestamps, time-zone preference, and
the legal name when the owner chose username display are omitted from public queries. Created
and last-edited times are visible only to the owning user.

## Campaign setup

A campaign has a name (3-80 characters), optional description (500 characters), player-slot
count (2-100), public or private visibility, optional labeled external links (at most 20
http/https URLs), a raster map image, and at least two factions. Each faction may have
subfactions. Optional ally groups may include two or more factions; every faction cannot belong
to a single ally group.

Setup may apply a faction preset. Applying a preset replaces the current faction and subfaction
list with an alphabetically sorted copy of that catalog entry. Later add/remove/rename edits
apply only to that campaign and do not change the preset. The initial catalog includes
Warhammer: The Old World. Setup can also clear the faction list (back to two empty slots) or
clear all ally groups.

The creating user is always a campaign manager (Game Master). If they also participate, they
occupy one player slot. Private campaigns store a hashed join password; the plaintext password
is never returned. Campaign names and faction names reject the same prohibited-language terms
as usernames. Members may read campaign metadata for campaigns they manage or play in. Only a
manager may edit or delete a campaign. Deletion removes the campaign from every member's list.
One map may be stored and later replaced; SVG and other active content are rejected.

## Campaign schedule and lifecycle

A campaign has a start date and time interpreted in a creator-chosen IANA time zone (UTC when
none is chosen). Instants are stored in UTC. Members see start, end, and current-phase times in
their personal display time zone.

Round count is 3-52. Each round has one length using minutes (1-60), hours (1-24), days (1-7),
weeks (1-52), or months (1-12). A round contains an ordered list of action windows and battle
phases. At least one action and one battle phase are required. Action lengths added together
cannot exceed the round length. Action and battle-phase lengths together must add up to the
round length, using calendar addition in the campaign time zone from the start instant. Campaign
end is start plus round length applied once per round.

The campaign state machine is derived from the server clock:

1. `Scheduled`: before the start instant.
2. `InProgress`: inside a configured round and phase. The current round number, phase, and
   phase window are included on the campaign page.
3. `Completed`: at or after the computed end instant.

A phase boundary belongs to the following phase. Action-window open/close and battle-result
rules remain as specified in later sections.

## Role and actor model

Administrator permissions include GM and player capabilities. Campaign GMs include player
capabilities and may simultaneously have a Player membership. Multiple GMs may exist.

When staff act for another party, record:

- actual actor: staff user performing the command;
- effective actor: player or neutral force represented;
- reason, timestamp, before/after values, revision, and notifications.

## Action-window lifecycle

1. `Open`: required participants save, commit, or uncommit.
2. `Closing`: a transaction freezes the required participant/order set.
3. `Revealed`: submitted/default orders become visible according to policy.
4. `Resolving`: deterministic precedence and conflict rules run.
5. `Resolved`: resulting map/battle state is committed once.
6. `Reopened`: staff correction creates a new revision and a new controlled editing window.

The final required commitment closes an open window atomically. Before that instant, a player
may uncommit. At the deadline, the latest valid draft is submitted. Missing slots become
`Hold`. Only users/forces that owe an order participate in the early-close calculation.

## Initial action vocabulary

- `Move`: travel to an allowed adjacent territory; invalid move becomes Hold.
- `Hold`: remain and receive applicable resting effects.
- `Build`: create an allowed structure in the current territory.
- `Pillage`: progress an allowed structure from operational to pillaged to destroyed.
- `Repair`: restore an eligible pillaged structure.
- `Split`: create a second force in an eligible adjacent territory; maximum two per player in
  the supplied rules.
- `Backstab`: terminate an alliance relationship and force battle under documented conditions.
- `Retreat`: move a losing/withdrawing force to an eligible territory or spawn fallback.
- `Battle`: automatic system action created by resolution; players do not submit it directly.

Battle overrides incompatible orders. If Action 1 puts a force in battle, later action slots for
that force become Battle.

## Battle lifecycle

`Pending -> AwaitingResults -> Finalized | Disputed -> GMResolved`

- Each participant may submit one current result; revisions retain history.
- Staff may submit on a participant's behalf with actual/effective actor attribution.
- Equivalent submissions finalize automatically.
- One timely submission becomes authoritative at the deadline.
- Conflicting submissions become Disputed and notify GMs.
- GM resolution preserves both submissions and appends an authoritative result.
- Three-or-more-participant engagements require a configured mission/result schema.

## Territory and structures

- Adjacency is a graph edge, not an assumption based only on touching pixels.
- Spawn locations prohibit enemy entry, battle, and construction unless configured otherwise.
- At most one structure occupies a territory under the supplied rules.
- Structure type, owner/controller, and condition are separate concepts.
- Conditions initially include `Operational`, `Pillaged`, and `Destroyed` where allowed.
- Cities may be pillaged but not destroyed in the supplied rules.

## Forces

- A force has location, controller, faction, supply context, current status, battle history,
  and optional relic possession.
- Split forces have independent orders, locations, supply paths, battles, and statuses.
- Two forces belonging to the same player rejoin when they occupy the same territory.
- A force has at most one status: Normal, Diseased, Exhausted, Well Rested, Shaken, or
  Confident, subject to faction exceptions.
- Neutral forces are forces, not user roles, and every neutral action identifies the GM actor.

## Supply

- Normal supply is calculated per force from spawn and connected territory/structure graph.
- Connected allied territory may participate when alliance rules permit.
- Temporary supply is a persistent, consumable player resource earned by configured events.
- Temporary supply is consumed only when an applicable battle occurs.
- Split forces may use the same connected structures as permitted by the campaign rules.
- Faction modifiers are data/rules layered over the base calculation.

## Objectives and relics

Objective visibility scopes: Public, Player, Faction, Alliance, Backstabber, and Staff.
Completion and awarded points are separate so a secret objective can be completed without
publicly revealing it.

A relic has hidden placement, revealed state, map location or possessor, choice/effect state,
and public history after reveal. Hidden location is never included in unauthorized queries.
Relics cannot return to spawn under the supplied rules.

## Corrections

A GM reopening or correcting a prior state never mutates history in place. It creates a new
campaign revision, identifies downstream state requiring recomputation/review, and notifies
affected users in-app and by email. Concurrent corrections must fail safely rather than use
last-write-wins.
