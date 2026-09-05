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
rejected). Usernames that collide with chat recipients or system keywords are reserved,
including everyone, public, private, here, all, channel, admin, and similar audience or
identity words. An account also has email, password or external login, legal name, optional middle initial, optional
name suffix (Jr., Sr., or Roman numerals I-X), city/region/country, IANA time-zone
preference, optional avatar, and a display-name preference. First and last names are at least
two characters and use the same prohibited-language rule as usernames. Local passwords must
be at least 12 characters and include uppercase, lowercase, a number, and a special character.
Created and last-edited instants are stored in UTC. The owner chooses a time zone for display
and a date-and-time format (Month Day, Year, Time Timezone with seconds by default, for example
January 5, 2027, 12:34:52 PM EST). When no time zone has been stored yet, those times are shown in UTC.
Campaign pages still convert instants in the campaign time zone; the profile format only changes how
the converted local time is written.

Other users may see username, location, avatar, and either the username or the full name
according to that preference. Email, created/updated timestamps, time-zone preference, date-and-time display format, and the
legal name when the owner chose username display are omitted from public queries. Created and
last-edited times are visible only to the owning user. Light or dark appearance is a client
preference stored in a cookie so it remains after sign-out; light mode is the default.

A public profile also lists campaigns the viewer may open: publicly viewable campaigns plus
private campaigns the viewer shares with that player. Scores and rankings are not shown until
those rules are implemented. Display names in chat, mentions, the Participants panel, and other
member lists link to that public profile. Profiles opened that way include a Back control that
returns to the previous in-app screen.

The application seeds Test 1 through Test 45 outside the automated Testing environment. Those
accounts cannot sign in with a password. An administrator may test as one of them from Test
users and return to the administrator session afterward. Test accounts receive in-app notices
only (never email), cannot change their profile, and cannot use public site chat. Campaign chat
is allowed. Their public display name is always `Test {n}`. On API start, if the privileged
administrator account is missing, it is created from `Identity:BootstrapAdminPassword` and
`Identity:BootstrapAdminEmail`.

## Campaign setup

A campaign has a name (3-80 characters), optional description (500 characters), player-slot
count (2-100), optional location (city, state or province, and country; all optional, but a city
requires a state or province and a state or province requires a country), public or private join
visibility, a publicly-viewable flag (on by default), optional labeled external links (at most 20
http/https URLs), a raster map image, and at least two factions. Each faction has a unique
color and may have subfactions. A faction may require players who choose it to pick a
subfaction; that flag may only be enabled when at least one subfaction is listed. Named
subfactions may inherit the parent color and flag or choose their own unique color, a color
flag, and/or an uploaded logo. The same uniqueness, tint, and 50×50 logo rules that apply to
faction flags apply to a subfaction when it chooses a color or logo. When a faction requires a
subfaction, each listed subfaction must have a unique color and either a color flag or an
uploaded logo (it cannot inherit). Optional
ally groups may include two or more factions; every faction cannot belong to a single ally
group. Each ally group has a unique color used for alliance map highlighting. On Edit campaign, each
ally group includes a faction dropdown that assigns named factions to that group and updates the
ally-group field on each faction in the Factions section. Renaming an ally
group keeps existing faction membership: factions stay in that group and show the new name.

Setup may apply a faction preset. Applying a preset replaces the current faction and subfaction
list with an alphabetically sorted copy of that catalog entry, including colors, whether a
subfaction is required, and any configured subfaction colors and flags. Later add/remove/rename edits apply only to that campaign and do not
change the preset. The initial catalog includes Warhammer: The Old World. In that preset,
Daemons of Chaos includes the subfactions Khorne, Nurgle, Slaanesh, and Tzeentch (alphabetical)
and requires a subfaction choice. Those four use unique colors (Khorne red `#B91C1C`, Nurgle
dark yellow-green `#3F6212`, Slaanesh pink `#F472B6`, Tzeentch teal `#0E7490`) and color flags.
On Edit campaign, expanding a faction shows that faction’s subfaction names, colors, flags, and
logo uploads on the same card. Setup can also clear the faction list (back to two empty
slots) or clear all ally groups. Armies of infamy are out of application scope as a dedicated
feature. They are ordinary subfaction configuration; if needed later they may be added to a
campaign preset or faction catalog.

Setup may apply a standard terrain preset or a standard structures preset the same way: applying
the preset replaces the current list with a copy of the current catalog values. Later catalog
edits in code update those presets, the same as faction presets. Setup may also apply a whole
campaign preset. The initial catalog includes The Hunt in Estalia, which copies the Old World
factions (including that preset's special-rule assignments), standard terrain (including water
feature flags), standard structures, the reusable special-rule catalog, and that campaign's
item-objective list (empty until named items are added). Applying a campaign preset fills the
campaign name only when the name field is empty. The Hunt in Estalia also copies the standard
force-status catalog (Diseased, Shaken, Confident, Exhausted, Well Rested). Normal is not a
catalog status; it is the absence of a status. Hunt also applies the default split-force supply
penalty (raw value 1), always-ask general-kill and supply-line questions (1 campaign point each),
and the per-round army size / free supply / free character table.

An administrator may save the current campaign settings as a named preset from Edit campaign or
Edit map. The dialog accepts a new name or autocompletes an existing saved preset or The Hunt in
Estalia. Names that match after trimming and collapsing whitespace overwrite the previous saved
preset instead of creating another. Saving a preset also stores the map image, overlay graph, and
uploaded catalog files (faction flags, subfaction logos, structure logos, and item-objective logos).
Applying a saved preset onto another campaign remaps overlay catalog identifiers by name onto that
campaign's terrain, structures, factions, and item objectives, and copies those uploaded files onto
matching catalog names. Saved presets appear in the campaign
preset list for later apply. A saved preset with the same collapsed name as a built-in catalog
preset replaces that catalog entry in the apply list.

Administrators may also download the current Edit campaign setup as a portable
`.mapandmuster-preset` ZIP (catalog, settings, overlay JSON, a visual overlay SVG, the original map
image, and referenced catalog files) and upload that file into another host's named-preset library.
Upload stores the package as a named preset; apply it with Add preset. Import uses overlay JSON only;
the bundled SVG is not executed or used as the overlay schema. Portable packages may be up to 64 MB.
User-uploaded maps stay at 20 MB; the stored PNG after re-encoding can be larger, and import accepts that
stored map up to the 64 MB package cap. Other uploads stay on the 24 MB host limit.

Optional item objectives may be none, one, or many (at most 50), each with a unique name.
Defaults are hidden until found, randomly placed, and not allowed on a spawn territory. A
Placed item is assigned to a territory in the map editor. Each item has campaign points (0–999)
awarded while a force currently holds it, a built-in logo chosen from ten generic symbols
(Crown, Sword, Shield, Chalice, Gem, Banner, Ring, Orb, Horn, Tome) with a color, or an optional
50×50 uploaded logo. Hidden items stay off player views until a force finds them or a manager or
administrator in debug mode clicks Reveal hidden objectives. Found or staff-revealed items stay
revealed. On the campaign map, uncarried items use the same pin treatment as structures. A force
that currently holds an item always shows that logo on its pin until the item is dropped or
transferred. Setup may also attach flavor text, named holder choices, and reusable special rules
to an item. Each choice has one result or a group of results; resolving the choice applies the
single result or one result picked at random from that group. A result may replace flavor text,
set a state label, grant a catalog private objective to the possessing player, destroy the item,
and/or spawn a replacement catalog item in its place. A destroyed item awards no campaign points
and is removed from the map and from every force.

Setup also configures campaign points on each terrain type (territory capture) and each structure
type (current holdings; destroyed structures do not count). Each terrain type has a Water feature
flag. The standard terrain preset marks Beach, Lake, Riverlands, Sea, and Swamp as water
features; other default types are not. The flag is display and special-rule metadata only; it
does not change movement or adjacency by itself.

Named public objectives live in their own setup section (name, optional description, and points).
Private objectives are a separate optional catalog (at most 50). Each private objective has a
name, optional description, campaign points, one or more holder kinds (player, faction, and/or
ally group), and either Manual or Automatic scoring. Automatic objectives name a criterion:
control a number of territories; control listed territories; control, pillage, or destroy a
number of a chosen structure type; win or lose a number of finalized battles; record a number of
player-chosen retreats (orders submitted by a player; delinquency defaults and staff corrections
do not count); occupy the same territory as a relic or a territory with a direct map connection
to it (any relic, or a named catalog item); build or repair a number of structures of a chosen
type, or of any type; control a relic (any, or a named catalog item); defeat an opponent in
battle (any, one random opponent chosen at assignment, or a specific faction or ally group);
or gain a force status a number of times, cause another force to gain a status, or gain a status
after gaining or losing another status. At launch, occupying players, factions, and ally groups each
receive one secret objective from that holder kind's pool. A holder kind with no configured
objectives receives none. Assignments in a pool are unique until that pool is exhausted; remaining
holders then receive duplicates from a newly shuffled copy of the same pool, repeating until
everyone in the pool has one. Duplicate catalog types are independent assignments: each recipient
must complete their own copy, and each revealed copy awards its points to that player, faction, or
ally group. A player who joins play later receives one player-pool objective the same way. After
launch, a manager may grant a specific catalog objective, or a random one from that holder's pool,
to a chosen player, faction, or ally group. Private-objective catalog
entries cannot be added after launch.

Points per finalized battle win are configured with public objectives (default 0). Battle-point
difference uses the campaign's configured multiplier and clamp; the application does not copy a
proprietary conversion chart.

A campaign has a reusable special-rule catalog (at most 80), each with a unique name,
description, and optional mechanical effect key. Factions and named subfactions may reuse the
same special rule, the same way terrain and structures reuse missions. Applying the Warhammer:
The Old World faction preset or The Hunt in Estalia campaign preset copies Hunt faction-sheet
rules onto matching factions (and daemon gods), including named effect keys the engine enforces.
User-created special rules omit an effect key and stay display-only: they do not execute code and
do not change map resolution. The manager writes the description for a custom rule. Daemons of
Chaos require a subfaction.

Named Hunt effect keys the engine enforces or calculates (matched by key, not display name):

- `Crusaders`: a Move may travel two adjacent hops. The order names the first territory and the
  landing territory. An enemy in the first hop stops the force there for battle. The first hop is
  not claimed. The route cannot enter another faction's spawn. Split forces rejoin only when both
  are moved into the same territory.
- `Slavers`: each owned unpillaged Town or City grants one extra map supply point.
- `DividedWeStand`: daemon-god subfactions of the same faction count as allies and may backstab
  each other by god.
- `OnlyBloodSatisfies`: Pillage may target an allied structure and may destroy it in one action.
- `BringersOfThePlague`: never Diseased or Well Rested; beating a force that is not Diseased or
  Shaken inflicts Diseased.
- `ArtOfWar`: Retreat may enter any non-enemy-spawn territory and may capture it.
- `ConduitsOfPower`: a player is told when they are adjacent to a still-hidden relic. After a
  relic is revealed, they may Move to any territory adjacent to it.
- `SpawningPools`: owned water-feature territories without a Town, City, or Castle count as a
  supply depot and fortification without a supply path. Built Supply Depot or Fortification
  structures grant one extra map supply point. The bonus does not apply to allies.
- `ToughGuts`: never Diseased.
- `GreenTide`: cannot Build a Supply Depot; owned empty or pillaged territories count as depots.
  Allied land is not included.
- `DefendersOfTheHomeland`: unowned Towns and Cities count as depots regardless of path. Allies
  still need a connected supply line.
- `GreatCityOfMagritta`: the force starts at and captures the Capital City.
- `UndergroundNetwork`: no spawn. The force is placed at random into an empty Town or City (or
  the Capital City if none are empty), capturing an empty Town or City that is not a spawn or
  capital. Occupying a spawn with another faction does not start a battle until they leave.
- `CalledByTheRelic`: after a relic is revealed, Move destinations are only those that reduce
  distance to the closest revealed relic (ties allowed) until the relic is captured or battle is
  forced.
- `Undead`: never Shaken, Diseased, Well Rested, or Confident.
- `NorthernRaiders`: Pillage awards at least two temporary supply points.
- `PreparedForBattle`: on a battle result, the player may declare Extra Black Powder; that spends
  one extra supply point for the battle.
- `MagicalSupply`: on a battle result, the player declares how many leftover unused composition
  supply points they used as one-per-battle casting or dispelling rerolls. Those leftover points
  are not spent from the campaign pool and cannot be saved for later battles.

Tabletop-only Hunt keys stay as catalog text and battle reminders: `ExpertAmbushers`,
`SafeInWater`, `Alluring`, `Treacherous`, `ItIsGoingInTheBook`, `RulersOfStone`,
`Determined`, `ForHire`, `RelicOfAPastAge`, `FreshCorpses`,
`NavigatorsOfTheForests`, and `HealedByNature`. The application does not resolve tabletop dice
or army-list mercenary slots.

A campaign may configure named force statuses (at most 20). Each status has a unique name other
than Normal, effect text shown on the force, an enable trigger, and a clear trigger. A force has
at most one status; Normal is stored as no status. Setup can copy the standard catalog:
Diseased (enable when occupying a water-feature territory; clear by Hold while not on water),
Shaken (enable after a lost battle or forced retreat, except a no-result neither-submission
forced retreat; clear by Hold), Confident (enable after a won battle; clear after a loss or
retreat), Exhausted (enable after any resolved battle; clear by Hold), and Well Rested (enable
after Hold; clear after a move or battle). Catalog order
matters when more than one enable trigger matches: the first matching status wins, so a loss
becomes Shaken rather than Exhausted. Effect text is display-only; the app does not resolve
tabletop modifiers. Named effect keys can refuse a status: `Undead` never Shaken, Diseased, Well
Rested, or Confident; `BringersOfThePlague` never Diseased or Well Rested; `ToughGuts` never
Diseased.

The creating user is always a campaign manager (Game Master). If they also participate, they
occupy one player slot. Private campaigns store a hashed join password; the plaintext password
is never returned. Publicly viewable campaigns may be opened by any signed-in user. When a
campaign is not publicly viewable, only players, managers, and administrators may open it after
it starts. Upcoming campaigns still appear on All Campaigns so players can join. Campaign names
and faction names reject the same prohibited-language terms as usernames.

The campaign page lists attached members in a Participants panel: each player's display name
(linked to their public profile), selected faction and subfaction when chosen, and roles
Manager, Player, and/or Admin when those apply. A manager or administrator may search accounts
(including test users) by username or display name, add a player to a public or private campaign
without the join password, promote an existing player to campaign manager, or bring in a user
who is not yet attached as campaign manager only or as both manager and player. A manager-only
member does not occupy a player slot. They may kick a non-manager player (which notifies them
in-app and by email unless they are a test account), and assign another player's faction and
subfaction from one dropdown that lists subfactions as
`Faction Name - Subfaction Name`. Choosing a value in that dropdown saves it. Player-managers
can be assigned a faction the same way; kick and promote stay limited to non-manager players.
From the third missed-order offence onward, staff also see a
**May be kicked** badge on that participant that opens the matching campaign-log entry. Players may
still change their own faction until the campaign starts; after launch only staff assignment
changes it. A kicked player's forces, drafts, and unresolved battles are removed, and carried
items drop on the territory they occupied.

Once a campaign is in progress or completed, near the bottom of the campaign page a Campaign points
panel lists every player occupying a slot. Upcoming campaigns omit this panel. Default order is
highest total to lowest, then display name. Columns are display name
(with currently held visible item-objective logos), faction logo, alliance group, Structures
captured, Battle points, Public Objectives, Private Objectives, Other, and Total. The five point
columns sum to Total. The table sorts by any of those columns. Structure points are the current
holdings (destroyed structures do not count). Battle points are cumulative campaign points from
resolved battles: by default the score differential (winner minus loser, times a multiplier,
clamped to a configured range, default 0 to 10) with draw participants each receiving configured
draw points (default 1). When differential scoring is off, a win awards configured win points
(default 2) and a draw still awards draw points. The loser receives negative points only when
that option is enabled. Public Objectives include manager-awarded named catalog items (award
and revoke are append-only facts; originals are never overwritten) plus ranking objectives
that currently award points to every player tied for first: most territories controlled,
longest unbroken chain of the player's own territories, most battle wins (draws break
win-count ties), and most structure campaign points from currently owned non-destroyed
structures. Running public objectives add configured campaign points for each currently owned
territory, and for each revealed relic currently held by another player of the same faction or
a current (not backstabbed) ally. Relics the scoring player holds stay in Other. A named,
ranking, or running objective configured at 0 campaign points is ignored.
The panel also shows a top five for each enabled public objective that is not an item objective:
ranking objectives, points per territory when that running objective is configured above 0, and
named catalog public objectives. Allied relic control is scored in Public Objectives but is not
shown as a top five. Each list has at most five rows. Tied players at first, second, or third
are listed individually when they still fit in five rows. A tied group that would push the list
past five is shown as "X players tied with Y", where X is how many players share that value and
Y is the value. Private Objectives is the
total of revealed or completed private-objective points that apply to that player: a
player-scoped award counts only for that player; a faction award counts for every current
player of that faction; an ally-group award counts for every current player whose faction is
still in that group. Each assignment is scored on its own, including duplicate catalog types.
Manual private objectives enter this column after a manager approves a
claim. Automatic private objectives enter it when their map criterion is met.
Unclaimed private objectives do not score when the campaign ends. Other is currently held visible
item-objective points. Destroyed items contribute nothing. Hidden items are omitted from
unauthorized standings and logos so the columns still add up for that viewer; the holder and
staff in an active debug session see their own hidden items.

Public knowledge of private objectives is the unclaimed count for each player, faction, and
ally group that has at least one assigned private objective still unrevealed. Names, text, and
progress of unrevealed private objectives are returned only to authorized holders (the player;
members of the faction or ally group) and to campaign managers or administrators. Revealed or
completed private objectives are listed publicly, and their points are included in the Private
Objectives total. When the campaign is completed, remaining assigned private objectives become
publicly visible for review, but still-unclaimed manual objectives do not add points.
Holders may still claim during the settlement window after play ends and before a manager
closes the campaign; automatic criteria continue to score if they are met in that window.

While a campaign is in progress, the map toolbar offers a display-only highlight mode for the
current viewer: configured overlay colors, faction colors, or alliance colors (unaligned
factions, and factions whose alliance was broken by Backstab, use their faction color). The
browser stores that highlight mode, which panels were expanded or collapsed, standings sort,
last chat recipient, and last chat scroll position in a per-campaign cookie (`cv-{campaignId}`,
Path=/, Max-Age one year, SameSite=Lax), following the same pattern as the color-theme cookie.
Map zoom (Fit vs a percent) is stored per campaign in `localStorage` under
`map-view-zoom:{campaignId}`.
Game state still refreshes from the server; only the viewer's layout is restored.

Your campaigns lists campaigns the user manages or plays in. All campaigns lists upcoming
campaigns plus publicly viewable active and completed campaigns, using the same grouping and
sort: active by soonest end, upcoming by soonest start, completed by latest end. Listings show
player slots occupied of maximum, name, description, filled location parts, proposed start and
end, and for active campaigns the current round, phase label (Action 1, Action 2, Battle, or
Battle N when a round has more than one battle), and a countdown until the current phase ends.
A public site chat sits above those lists. It is not stored on any campaign play log.

A signed-in user may join an upcoming campaign that still has an open player slot. Public
campaigns join without a password; private campaigns require the join password. A manager or
administrator may add a player without that password, including after launch while slots remain
and the campaign is not completed. Members who are not managers may leave. Managers edit instead
of joining. Listings open the campaign page with Open. There is one campaign page: a member's
role and involvement decide which controls appear.
Players submit orders and battle results there. Managers also see schedule extension, ringer-battle
injection during an open battle phase, and managers or administrators can enter a logged debug
session to correct orders, re-resolve the previous action while the following phase is still open
(or during the post-campaign grace), or override battle results. Upcoming
and completed campaigns use the same page without live order controls. The campaign page includes
a collapsible public log and member chat for upcoming, in-progress, and completed campaigns.
Your campaigns also offers Duplicate campaign on every listed campaign. Duplication copies the map overlay, factions, missions, ally
groups and their colors, special rules, public and private objectives, battle scoring, ranking public objectives, item-objective types (including flavor text and choices), links, visibility, location, and schedule template into a new campaign whose start is one
week after the duplication instant in the campaign time zone. The duplicating user becomes the
manager of the copy and occupies a player slot only when they were a player on the source.
Raster maps, flags, structure logos, item-objective logos, and mission files are reused until the copy replaces them;
the overlay SVG data is copied. Play state, memberships, and unresolved orders are not copied.

Only a manager may edit or delete a campaign. Deletion removes the campaign from every member's
list. A raster map image is required when creating a campaign; SVG and other active content are
rejected. Maps may be JPEG, PNG, or WebP up to 20 MB. One map may later be replaced; the previous
map file is deleted when it is no longer used. Deleting a campaign also deletes its stored map and
user-uploaded catalog images. Built-in structure icons are application assets and are never deleted.

Setup sections (details, schedule, visibility, ally groups, factions, subfactions, special
rules, force statuses, terrain, structures, missions, item objectives, public objectives, private
objectives, links, and map) can be expanded or collapsed. Section actions collapse
with their section. Invalid sections expand automatically when save validation fails. Create and
edit start with Campaign details, Schedule, Factions, Terrain types, and Campaign map expanded;
optional sections start collapsed. A wide-viewport section index lists every section and marks
required ones that still need work, and the sticky toolbar shows how many required sections remain.
Nested terrain and structure mission groups use unique names such as "Missions for Beach." Setup
keeps Back to campaigns, Expand All, Collapse All, and Save or Create in a
sticky toolbar. Edit campaign also includes Edit map, which opens the map editor without saving
the current form. Edit map is offered only while the campaign is Scheduled. Opening the editor
after a campaign has started returns to the campaign page with a message that the map can no longer
be edited.

The campaign page keeps Back to campaigns, Expand All, Collapse All, and (when allowed) Debug
in a sticky toolbar. Edit campaign and Edit map sit under Manage campaign and are offered only
while the campaign is Scheduled. Every panel except the status strip can be
expanded or collapsed. Status sits above the other panels and shows the current round and phase,
lifecycle status, remaining phase time, and the phase-end timestamp. The live map, orders,
battles, and schedule controls appear on this same page according to the viewer's role. The
campaign page refreshes round, phase, and status when the server clock advances a window; a
refresh is not required.

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

1. `Scheduled`: before the start instant, unless a manager has already closed the campaign.
2. `InProgress`: inside a configured round and phase. The current round number, phase, and
   phase window are included on the campaign page.
3. `Completed`: at or after the computed end instant, or immediately when a campaign manager or
   administrator ends the campaign.

A manager or administrator may end a campaign from the campaign page or Edit campaign. Ending
closes play immediately and keeps the campaign stored in its final state: remaining orders are
not resolved, files are kept, and members can still open logs, standings, and duplicate the
campaign. Ended campaigns appear in the Completed group. All current members are notified in-app
and by email. The original scheduled end instant is left unchanged; list ordering for completed
campaigns uses the close instant when one is recorded.

A phase boundary belongs to the following phase. After launch, actual phase windows are stored
and may diverge from the original template when a window closes early or a manager extends it.

After the start instant, managers cannot change the map, the ordered action and battle steps,
name, description, factions, faction abilities, special-rule catalog, ally groups, terrain,
structures, missions, private-objective catalog, or most other setup. They may still grant
catalog private objectives and approve or deny claims. They may increase the number of rounds, not below the current round and not
above 52. They may lengthen the current round by adding time to the current or remaining action
and battle windows; a window cannot be shortened below the duration already in effect for that
window. Added rounds use the original phase template and make the campaign longer.

Each action window and battle phase has an "End phase early if able to resolve" checkbox,
default on. When it is on, a window that can resolve closes immediately and the next window
opens with that next window's duration already in effect, not leftover time from the window
that just ended. Later windows keep their scheduled start and end times so the campaign does
not finish early. When the checkbox is off, the window stays open until its deadline even if
nothing remains to resolve, so a manager can still inject a ringer battle during a battle
phase. Simultaneous-action resolution runs immediately when an action window closes.

## Role and actor model

Administrator permissions include GM and player capabilities. Campaign GMs include player
capabilities and may simultaneously have a Player membership. Multiple GMs may exist.
Administrators may also test as seeded Test 1–Test 45 accounts; the original administrator
identity is kept on the session until they return.

When staff act for another party, record:

- actual actor: staff user performing the command;
- effective actor: player or ringer battle represented;
- reason, timestamp, before/after values, revision, and notifications.

## Action-window lifecycle

1. `Open`: required participants save a draft for every force, then commit or uncommit.
2. `Closing`: a transaction freezes the required participant/order set.
3. `Revealed`: submitted/default orders become visible according to policy.
4. `Resolving`: deterministic precedence and conflict rules run.
5. `Resolved`: resulting map/battle state is committed once.
6. `Reopened`: staff correction creates a new revision and a new controlled editing window.

The final required commitment closes an open window atomically. A player may commit only after every required force that is not in battle has a saved draft. Before that instant, a player
may uncommit a committed order back to draft. At the deadline, the latest valid draft is
submitted. Missing slots become `Hold`. After the window closes, orders resolve and cannot be
returned to draft. Loading or mutating play state advances every overdue window in one pass, so a
campaign that sat idle past several deadlines catches up without a reload. Each force requires an action unless it is already in battle; same-player forces that occupy one territory
rejoin into one surviving force and therefore one later action. Only users/forces that owe an
order participate in the early-close calculation. If every remaining force is in battle, nobody
owes an action and the window closes early when that setting is on. An action window with no
forces at all waits for the deadline.

Players pick a faction, and a required subfaction when the faction demands one, before they
receive a starting force. On the campaign page, faction assignment (a player's own choice or a
manager assigning another player) uses one dropdown. Named subfactions appear as
`Faction Name - Subfaction Name`. Factions that do not require a subfaction still include the parent
name as a choice. Every player (including a manager who occupies
a player slot) must choose that faction before they can play. A participant who has not chosen a
faction may do so until they have one and cannot submit orders for a round until they do. A player
may change their chosen faction until the campaign starts. After the campaign has started, a chosen
faction cannot be changed. Each player force starts at that faction's spawn territory
when the campaign launches or when they join play later; required subfactions use that
subfaction's spawn when one is assigned, otherwise the parent faction spawn, unless a
named effect key relocates them (`GreatCityOfMagritta` to the Capital City,
`UndergroundNetwork` to a random empty Town or City).

Orders resolve simultaneously against the window's starting map state. Processing order is
movement and splits, then backstab alliance breaks, then battles from enemy co-location, then
`Build`, `Pillage`, and `Repair` for forces that are not in battle. An invalid `Move`, `Split`,
`Build`, `Pillage`, `Repair`, or `Backstab` becomes `Hold`. A force may not enter or claim
another faction's spawn. After movement, enemy forces that occupy the same territory create a
battle; later action slots for those forces become `Battle`. Same-player forces that share a
territory rejoin. Uncontested occupation claims a non-spawn territory and plants that faction's
flag, except: a spawn always keeps its faction's flag; a force cannot claim an ally's territory
or structure without backstabbing first (the previous owner's flag stays while the ally defends);
two or more allied factions on Neutral land award the claim to the strongest using the retreat
collision ranking. Enemy capture leaves the structure operational unless a configured special
rule auto-pillages it. The territory owner owns the occupying structure. Collisions that still
lack a documented ranking, including competing `Build`, `Pillage`, or `Repair` actions on the
same territory and competing arrivals, become `Hold` rather than an invented winner.

## Initial action vocabulary

Player-submittable actions in an open action window are listed in this order:

- `Hold`: remain and receive applicable resting effects.
- `Move`: travel to an allowed adjacent territory; invalid move becomes Hold. `Crusaders` may
  name a first hop and a landing territory two steps away. `ConduitsOfPower` and
  `CalledByTheRelic` can add or restrict destinations after a relic is involved.
- `Build`: create an allowed structure in a non-spawn territory that has no intact structure.
  Only structure types flagged buildable may be chosen. Town, Capital City, City, and Castle
  start not buildable; Supply Depot and Fortification start buildable.
- `Pillage`: progress a pillageable intact structure from operational to pillaged. The acting
  force may pillage a structure its faction owns. Allies cannot pillage an allied structure unless
  `OnlyBloodSatisfies` applies, which may also destroy in a single Pillage. `NorthernRaiders`
  awards two temporary supply points rather than one. A second Pillage against a pillaged
  structure that is flagged destructible removes it from the map. Capital City starts not
  pillageable. Capital City, City, and Castle start not destructible.
- `Repair`: restore a pillaged structure. Only the current territory owner or a current ally of
  that owner may repair.
- `Split`: create a second force in an eligible adjacent territory; maximum two per player in
  the supplied rules.
- `Backstab`: terminate an alliance relationship. It is only available when the acting force
  shares a territory with an allied force, or occupies an allied faction's territory that has no
  allied force present. If the acting force occupies a former ally's
  territory and no former-ally force (and no other remaining ally of that former ally) is there,
  the force claims that territory and auto-pillages the structure when it is pillageable;
  auto-pillage never destroys. If the backstab forces a battle instead, there is no auto-pillage;
  a later win is a normal operational capture.

Battle-phase and system actions:

- `Retreat`: move a losing/withdrawing force to an open (unoccupied) Neutral territory, a
  territory the force owns, or a territory owned by a current ally, otherwise to spawn. Players
  submit retreat after a battle, not during an action window, except as part of surrender.
  `ArtOfWar` may retreat into any non-enemy-spawn territory and may capture it.
- `Surrender`: while a force is engaged, during an action or battle window, commit to leaving
  the fight and retreat. Once committed it cannot be withdrawn. A surrender left in draft still
  executes when the window ends.
- `Battle`: automatic system action created by resolution; players do not submit it directly.

Battle overrides incompatible orders. If Action 1 puts a force in battle, later action slots for
that force become Battle. That force does not submit an action until the battle is resolved;
Surrender is offered on the battle, not as a required action-list item.

## Battle lifecycle

`Pending -> AwaitingResults -> Finalized | Disputed -> GMResolved`

Players may report a battle during the Battle phase or earlier while an Action window is still
open if they are already in that battle. One player reports both sides. The report includes
victory points, army size in points, how many supply-costing units they fielded (special, rare,
and similar; each unit spends one supply point), differential battle points from VP, bonus
battle points from the mission, and any mission result questions the campaign manager configured
(true/false or a battle-point amount, each awarding battle points and/or campaign points).
A force with Prepared for Battle may declare Extra Black Powder (spending one extra supply
point). A force with Magical Supply may declare leftover unused composition supply used as
casting or dispelling rerolls this battle only. A player may optionally paste army-list text for each force. That text is informational: the
opponent and campaign manager can read it to check the list by hand. The player may also choose
Warhammer: The Old World and a builder. Other (the default) does not parse the text. New Recruit
and Old World Builder attempt to recognize that app's text export and fill army points plus
supply amounts for Characters, Core, Special, Rare, and similar categories. Special, rare,
mercenary, and allied units default to one supply point each; the player may correct those
amounts. If the text cannot be parsed, the player is told to enter supply points manually.
Automatic parsing is only implemented for Warhammer: The Old World.
The campaign manager can keep a reusable catalog of standard battle-result questions (prompt,
true/false or battle-point amount, standard battle points, and standard campaign points) and
attach those questions to any number of catalog missions, including all missions at once.
Attaching from the catalog copies prompt and kind from the catalog item; that mission may
override battle points and campaign points. Unique per-mission questions remain available.
Old campaigns that stored always-on general-kill and supply-line questions keep asking them
because those flags migrate into catalog questions attached to every mission. New campaigns
start with an empty catalog. The
application spends reported supply-costing units first from that force's territory/structure
allowance plus the round bonus, then from the player's temporary pool.

- Each participant may submit one current structured result covering every participating force;
  revisions retain history.
- The other participant may agree with the reported result or submit different numbers.
- A campaign manager or administrator may be the second confirmation.
- Equivalent submissions (including an accept) finalize immediately.
- One timely submission becomes authoritative at the battle-phase deadline.
- Conflicting submissions become Disputed, lock the forces in that battle, and notify managers
  in-app and by email until a manager confirms (and may edit) the true result.
- Staff may submit on a participant's behalf with actual/effective actor attribution.
- Winner is the higher total battle points (differential + bonus + answered question BP). A
  true battle-point tie is not a loss: both forces must retreat. Otherwise only the loser
  retreats, dropping a carried item objective for the winner to pick up.
- Players submit retreat after the result is committed, by the end of the Battle phase. Eligible
  destinations are open (unoccupied) Neutral territories, territories the force owns, and
  territories owned by a current ally. The current battlefield and enemy spawns are never
  eligible, and a force cannot retreat onto a hex occupied by an enemy. Friendly occupation of
  owned or allied land is allowed (rejoin). `ArtOfWar` may also enter any other non-enemy-spawn
  territory and may capture it. A missing retreat, or a force with no remaining eligible
  destination, is assigned to that force's spawn. If two or more enemy forces would
  occupy the same territory after retreat, the strongest stays and the others are sent to the
  next safest eligible destination. Strongest is most current campaign points, then most
  territories, then most structures, then most supply including remaining temporary supply;
  a remaining tie is chosen at random and recorded on the play log.
- Surrender may be committed while engaged during an action or battle window. A committed
  surrender cannot be uncommitted. In a 1v1 fight the remaining player wins with maximum
  differential battle points (the scoring clamp, default 10) and no extra or mission bonus
  battle points. In a larger fight, allies of a surrendering force may keep fighting or also
  run. If only one side still has a fighting force, that side wins the same way. If every
  remaining force runs, the battle is a no-contest: nobody wins, ranking does not record a
  win or draw, and relics do not transfer.
- When more than one player fights on the same side, that side's round army-point cap increases
  by 25 percent per extra player, then is divided evenly and each force's share rounds up to
  the next 10. More than two opposing sides who do not all retreat: the two strongest play the
  first tabletop game, then remaining opponents play strongest-to-weakest in that same battle
  phase. A force that never received a game stays in the territory, still in battle, for the
  next round's battle phase. If a correction adds a force to an already fighting pair, keep the
  current report; the new force waits and then plays whoever remains as in this multi-force
  sequence rather than re-pairing by strongest first.
- If neither side submits a result by the battle-phase deadline, the engagement is a no-contest:
  every force that fought that tabletop game, including silent allies, is forced to retreat.
  Those retreats are chosen after all other retreats in the phase, using safest remaining
  destinations, then spawn. There is no win, draw, battle campaign points, or relic spoils, and
  Shaken is not applied. One timely submission remains authoritative; the silent opponent is not
  given a missed-result offence. Waiting other sides stay in the territory. If an ally of the
  previous owner remains, that owner's flag stays (the ally is only defending). Otherwise a
  remaining uncontested occupant claims, or remaining opponents start a new battle.
- A battle phase ends early when its "End phase early if able to resolve" checkbox is on and
  every engagement is finalized and every required retreat is recorded, and also when that
  checkbox is on and no battles remain for anyone to report. The next window then runs for its
  own duration rather than leftover time from the battle phase. When the checkbox is off, the
  window stays open until its deadline. The battle-phase commitment count is unique players who
  have a force in a battle this window. A player with two forces in two battles is one of that
  total and is not committed until every one of their battles has an agreed result or a
  manager/administrator entered or confirmed the result on a participant's behalf, and any
  required retreat is recorded.

## Territory and structures

- Adjacency is a graph edge, not an assumption based only on touching pixels.
- Spawn locations prohibit enemy entry, battle, construction, and capture. The spawn faction's
  flag is always present there.
- A faction that controls a non-spawn territory displays its flag there. Neutral means no
  faction owns the territory.
- At most one structure occupies a territory under the supplied rules.
- Structure type and condition are separate from each other. Structure ownership follows the
  territory owner; there is no independent structure owner.
- Each structure type has Buildable, Pillageable, and Destructible flags configured in campaign
  setup.
- Conditions are `Operational`, `Pillaged`, and `Destroyed`. Setup and the map editor may place
  a structure as Operational or Pillaged. Play may destroy a pillaged structure that is
  destructible; destroyed structures are removed from the map so a later Build can occupy the
  empty territory.
- A pillaged structure is shown with its pillaged icon and labeled as `Name (pillaged)`, for
  example `Town (pillaged)`. Repair restores the operational condition and operational icon.
- Capital City starts not pillageable and not destructible. City and Castle start pillageable
  but not destructible. Town starts pillageable and destructible.
- Castle siege mechanics (gates, walls, battering rams, scaling, and inside-the-walls
  behavior) are out of application scope. Castle remains a structure type. Tabletop castle
  features stay display-only mission or terrain text; the application does not simulate siege.
- Each structure type has a built-in operational icon and a built-in pillaged icon. Campaign
  setup may replace either with a user-uploaded 50×50 logo.

### Map overlay editor

After campaign creation, the creating manager is taken to the map editor. Select, Draw, Erase, and
Connect are an exclusive toolbar group with icons; Select is the default mode and is listed first.
The active mode uses a glow highlight, not the
Save Map accent. Command groups are labeled Tools, Edit, Connections, Colors, and File. Destructive
commands use the danger confirm pattern. Undo, redo, and Close Territory sit in Edit. The Territories
list starts expanded. There is no Cancel Drawing control; switching
to Erase, another tool, or pressing Escape clears an in-progress drawing. Draw, connect, color, and
save controls stay sticky at the top of the viewport while they fit; when the toolbar is taller
than 40 percent of the viewport it scrolls inside itself. Territories are drawn as
an overlay on the rectangular raster map; the image itself is not modified. Overlay coordinates are
normalized to the unit square. Drawing stays inside the image rectangle. Territories may share a
border, including when a drawn trace sits slightly along that border. Interiors that actually
cover each other cannot be saved.
The drawing cursor highlights when it is about to snap
to an existing vertex. Managers may undo, redo, or erase segments, assign an optional unique name and
description (otherwise the display number 1, 2, 3… is used), select a required terrain type,
select at most one optional structure and whether that structure starts Operational or Pillaged,
assign optional ownership (otherwise Neutral), assign an optional spawn faction (at most one
spawn per faction, or per required subfaction when that faction requires a subfaction choice),
place catalog item objectives that use Placed launch placement, and apply a
transparent overlay color. Setting a spawn always sets ownership to the same faction or required
subfaction. The spawn list disables factions whose special rules include `UndergroundNetwork`
because those forces have no fixed spawn. Factions that require a subfaction are listed as
"Faction Name - Subfaction Name" rather than the parent name; map flags and colors use that
subfaction's chosen color and logo when configured, otherwise the parent faction's logo and color. The collapsible
Territory editor below the map keeps a fixed field area, tall enough for its three field rows
without a scrollbar, while it is open so hover, selection, deselection, zoom, and drag on the map
do not shift the map up or down the page. Fields are name
and description on the first row, terrain, structure, structure condition, and overlay color on the
second, and ownership, spawn, and delete on the third. Hovering a territory does not open those
fields; a selection does. The Territories side panel is a narrow sliver with an expand control until opened, then
grows horizontally to list territories. Each row shows the owning faction's mark (uploaded logo,
tinted when that setting is on, otherwise a color flag), then an optional structure symbol of the
same size when a structure is present, then the terrain-type symbol, then the territory name, inside
a bordered list button. When expanded, that list scrolls and matches the height of the map column
(map plus the Territory editor beneath it). Selecting a territory that is outside the visible list range scrolls to it; a
multi-selection scrolls to the topmost selected name. Closing the list expands the map horizontally;
map height stays the same. The editor map is taller than the campaign-page map.

Auto Generate Connections suggests adjacency arrows from shared borders. It uses the same secondary
button style as Clear Connections and the other toolbar actions. User-created (manual) arrows
are kept on regenerate, and those pairs are skipped. Generated arrows may be replaced. Managers may
add or delete arrows, including generated ones, and may clear all arrows. A pair of territories has
at most one connection, and every connection is between exactly two territories. Select two
territories and click Connect, or use the Connect tool and click two territories. Selecting two
territories that already share a connection shows that connection in the editor bar so a manager can
delete it. Selecting one territory lists its connections there; a manager may remove a connection
from that list. Connection arrows
are selectable only in Select, where clicking an arrow shows both territories in the collapsible
editor bar above the map so the pair can be changed or deleted, and in Erase, where clicking an arrow
deletes it. Drawing and
Connect ignore arrows so they do not intercept the pointer or affect overlay drawing. Arrow markers
are editor aids and are not part of the published map image. Each connection arrow stretches across
the shortest gap between its territories so adjacent arrows do not cross. The arrow head and up to
10 pixels of the shaft overhang each territory so the heads stay visible and selectable in Select
and Erase. Visible connection arrows are black and keep their resting size and outline. Hovering an
arrow in those tools glows both connected territories without washing the rest of the map.

Campaign setup owns the terrain-type and structure catalogs. The initial terrain types,
alphabetically, are Beach, Cave, Desert, Forest, Highlands, Jungle, Lake, Mountain, Plains, Riverlands, Sea,
and Swamp.
Each has a unique color, a symbol, at least one mission, and a Water feature flag. Beach, Lake,
Riverlands, Sea, and Swamp start as water features. Setup starts each terrain type with one
empty mission row. Hovering a territory shows whether its terrain is a water feature. The initial structures, alphabetically, are Capital City, Castle, City, Fortification, Supply
Depot, and Town. Town, Capital City, City, and Castle are not buildable; Supply Depot and
Fortification are. Capital City is not pillageable. Capital City, City, and Castle are not
destructible. Each structure uses either a built-in icon or an uploaded logo image, not both.
Clearing or replacing an uploaded logo deletes only that uploaded file. Built-in icons remain in the
application.
Uploaded structure logos are limited to 50×50 pixels; larger images are shrunk to that size.
Structures start with no missions. Structures may have zero or more missions. Reusable missions are
configured in the Missions catalog: name, an optional http/https URL or stored PDF/Word file, result
questions, and optional attacker/defender flags. Terrain and structure lists attach a catalog mission
or a one-off name with an optional file. A territory uses its structure missions when that structure
has any; otherwise it uses its terrain missions. When a battle is created, one mission is chosen at
random from that pool. Attacker/defender missions are used when one force Held or retreated into the
territory and another Moved or Split in (not Retreat), when a force defends a structure it or an
ally owns, or when a force backstabbed its opponent. An ally standing on allied land defends it
and may use an attack/defend mission; winning does not transfer ownership. Otherwise they are
used only if no normal mission exists, and attacker/defender roles are then assigned at random.
Role priority is backstab, then structure owner, then Hold/Retreat versus Move/Split.
Chosen missions appear on the campaign Battles panel with the mission name, attacker/defender or
pitched-combatant roles, army and supply points for each reporting force, and a link or file
download that opens in a new tab when present. If the mission has no URL or uploaded file, the
panel says "See Campaign Manager for Mission details." Attacker/defender missions may grant a signed army-point number
or percent change and a signed raw supply-point change to one side; after apply, army points are
never below 500 and supply points are never below 1. Mission names are unique across the campaign.
Unassigned catalog missions are kept. Mission attachments are an http/https URL or a stored PDF/Word
file, not both. An already configured mission may be selected again for another terrain or structure
instead of uploading a duplicate file. New uploads and reused missions may be mixed.

Hovering or selecting a territory, while editing or viewing, shows Name, Description, structures,
ownership (or Neutral), spawn-location faction, adjacent territories, terrain, and missions.
In Select mode, a mouse click on a territory keeps that territory selected. Ctrl+click, Shift+click, or Command+click
adds or removes territories from the selection. Dragging on empty map draws a selection box that
selects every territory it intersects; the box is 50% transparent gray with a black 2px dashed
border and disappears when the button is released. Hold Control, Shift, or Command while dragging the box
to add to the current selection. A click on the map with no territory clears the
selection. When two or more territories are selected, the editor bar above the map lists all of them and the map
highlights each as selected. Selected territories can be deleted with Delete or the Delete territory
control, including when several are selected. Dragging a selection moves those territories together
and keeps them on the map. Shared borders are allowed; dropping a moved group still cannot overhang
into another territory's interior. While a group is being dragged, it is highlighted green
at about 70% fill with a centered checkmark of at most 50×50 pixels when it can be dropped, or red
at about 70% fill with a centered X of at most 50×50 pixels when it cannot. Dropping while red
restores the group to the position it had when the drag started.
Owned territories also show a flag at up to 50×50 pixels, using the faction or subfaction color flag by default or an
uploaded image. When logo tinting is enabled for that faction or subfaction, the uploaded logo is filled with the
resolved color wherever it is shown, including the map. Untinted uploaded logos keep their original colors.
Structure logos and ownership flags sit at 50×50 pixels in the
territory center when that size fits; otherwise they shrink and shift to stay inside the polygon, as centered
as possible. When a structure is present, the ownership flag sits beside it. Force markers sit in the
territory as colored dots. They stay off structure logos and ownership flags, shrinking as far as 50% of
their usual size when needed so they remain visible without covering those markers. Highlights use a subtle
contrasting glow. Selected territories use a full highlight: about 70% fill and double the usual
border thickness. Hovered territories, possible action destinations, and territories connected to
the current selection use a half highlight: 50% fill and 1.5 times the usual border thickness.
When a territory qualifies for both, the full highlight wins. When several territories are selected,
those territories stay fully highlighted and every other connected territory uses the half highlight.
Hovering an unselected territory, while not drag-selecting, lifts that polygon a few pixels so the
hovered territory is easier to distinguish. The lift eases in and out over 200ms on the hovered
territory only. Hover only starts or stops after the pointer stays on or off the territory for a
short moment, and the painted shape moves independently of the pointer hit area, so borders do not
flicker. Zooming does not animate every territory. In the map editor, that hover lift and hover
highlight
do not apply in Draw mode. Spawn territories fill with 5-pixel-wide diagonal stripes
of the overlay or spawn-faction color, using the same highlight opacities as a solid fill.
When a territory
has an overlay color, that color is the glow, strongest around the territory border. Overlay color
mode is Random Colors, Color By Terrain, or Manual Colors, shown as an exclusive toolbar group; the
active mode uses the same filled accent style as the active map tool. The last chosen mode for a
campaign is restored when the map editor is opened again, without recoloring existing territories
until Random Colors or Color By Terrain is chosen again. Switching to Random Colors or Color By
Terrain recolors every territory. A new territory, or a terrain change while Color By Terrain is on,
uses that mode's color. Remove Colors switches to Manual Colors and clears every overlay color.

The map overlay starts fitted to the panel. Map panels are full width in their parent. Main page
content and the footer share a 90 rem maximum width. The right-hand column of both the campaign map
and the map editor is 22 rem; toolbar and directory controls wrap so that column does not scroll
horizontally. On the
campaign page, the Territories directory beside the map is collapsible. That heading stays at the
top of the right-hand column when collapsed, and the list scrolls inside the map height when
expanded. Directory rows use the same bordered layout as the map editor: owning faction mark,
optional structure symbol, terrain-type symbol, then territory name. Selected-territory details sit under the map in that left column rather than spanning the
directory. Zoom
controls sit across the top of the map in this order: zoom percent field, +, -, Fit, 100%, and Full
screen. Zoom is 10% to 800% of the map image's actual pixel size, in 10% steps. 100% shows the
image at its native size and centers it. Fit scales the image to the view and recenters it.
The F key fits the map; 1 (and 0) set 100 percent. N toggles Show names. Zoom defaults to Fit. After the viewer changes
zoom, that Fit-or-percent choice is restored the next time the same campaign's map opens. M toggles full-screen map mode on the campaign
page and map editor while the map is shown; Escape exits full screen. Full-screen mode keeps the map
inside the viewport: a fitted map recenters when the panel size changes, and a zoomed map clamps pan
so the image cannot sit off-screen. Selecting a territory or group from outside the map (the
directory, campaign links, or the map editor list) pans to center that selection as far as image
bounds allow. Zoom changes only when the current scale cannot show the whole selection, and never
zooms out past Fit. The first time a map view
opens, the panel shows a loading ellipsis and hides overlay markers until that image has loaded so
they do not cluster in the corner. Hover, selection, and later map updates do not show it again. Drawing coordinates stay normalized to the full-size image. Snap distance, minimum draw spacing, and
overlay stroke widths are measured in screen pixels so zooming in lets a manager trace fine coasts and
province borders. Territory names drawn on the map stay a readable screen size at Fit zoom and use
the current theme's surface and text colors. Show names (N) draws the full territory name even when
the polygon is small, and hides a display number when that number would not fit. Hovering a territory
on the map or a row in the Territories list shows a tooltip with the territory name, owner or Neutral,
structure type and pillaged state when a structure is present (`Town` or `Town (pillaged)`), terrain
type, forces in the territory, whether a battle is to be had there, and any force still there that
lost or surrendered and is retreating. When the zoomed image is larger than the panel, it can be panned
but not dragged past the image bounds. Hold a right-click (context-click) or middle-click and drag to
pan without drawing, erasing, or selecting; the mouse wheel still zooms the same way it does with any
tool. Left-click drag does not pan. On a touch screen, pinch with two fingers to zoom and drag with
two fingers to pan. Arrow keys and space-drag also pan. Drawing territories requires
a pointer; other map controls, including zoom, pan, tool choice, and the territory list, are keyboard
accessible. A click on empty map, including the letterboxed panel around a fitted image, clears the
territory selection. Only the hovered or selected arrow glows. Saving the map graph does not change
zoom, pan, or fit. Save Map is disabled until there are unsaved overlay edits. After a successful
save, the green banner "Successfully saved changes." is followed by the last-saved time, and a green
checkmark sits to the right of Clear Unsaved Changes, before that timestamp. A failed save shows a
red X in that same place. Clear Unsaved Changes is disabled until there are unsaved overlay edits; it
discards those edits, restores the last saved graph without resetting zoom or pan, and clears the
checkmark or X. Overlay fields that differ from the last saved graph show a small orange (#C87606)
triangle in the top-right corner, including placed-item
checkboxes. Dirty territories are also marked that way in the territory list. The Map editor title
is not marked dirty. Territory names and terrain symbols in the editor list use the same light text
color as other dark-background fields in dark mode.

The instruction paragraph under the toolbar is omitted. Editable territory fields sit in a
collapsible horizontal bar below the map and zoom controls. The territory list stays in the side
panel as a collapsible toolbar; when expanded it scrolls, and its max height matches the map, zoom
controls, and territory-edit section combined so the list never extends below that column. Selecting a
territory that is outside the visible list range scrolls that row into view; a multi-selection
scrolls to the topmost selected name. Zoom controls include Show Overlay (on by default) and
Show Connections (on by default). Turning off Show Overlay hides the territory overlay, markers, and
connections. Show Connections has no effect while Show Overlay is off. When one or more territories
are selected, other territories that are not selected, hovered, or connected to the selection drop
to 25% opacity. Connection arrows that touch a selected territory are white with a thin black border.

Edit campaign Save campaign is disabled until the form or pending uploads differ from the last saved
campaign. Clear Unsaved Changes is disabled until then and restores the last loaded campaign, including
clearing pending map, flag, structure, item, and mission files. Dirty fields show a small orange
triangle in the top-right corner. Collapsed section and subsection headers that contain at least one
dirty field use an orange underline instead. The Campaign map title is underlined whenever a new map
file or preset map is pending. Pending uploaded files and edited link fields are marked dirty as well.

Edit campaign shows a static 200×113 map preview of the current image. It is not
zoomable or selectable. The campaign page shows the interactive map at full width, with territory
details under the map. The campaign page Factions section lists each faction name as a map-focus
control. When that faction has a fixed spawn territory, the territory name follows in parentheses as
a map link. Required-subfaction spawns that differ from the parent spawn are included in the same
parentheses, alphabetically, as `Subfaction: Territory`. Factions with no specific spawn omit that
parenthetical. The campaign page Ally groups section lists groups alphabetically. Each group name is a map-focus
control, followed by the current player count in parentheses, then its member factions in
alphabetical order. Catalog subfactions for a faction appear in parentheses after that faction,
also alphabetical: `Alpha League (1 player) - Midland (East)`. Players currently in the group appear as
nested bullets in display-name order, each with their chosen faction and subfaction when they have
one: `Bob (Midland, East)`. Players who have not chosen a faction, non-player members, and
backstabbed factions are omitted from the count and the nested list. Clicking a player, faction, or
ally group on the campaign page, while the player is not issuing an order, highlights that party’s
territories and emphasizes their forces on the map. Force markers stay inside their territory,
or as close as possible without sitting on a neighboring territory. Clicking the same party again clears that focus.
A profile link still opens the user profile. The campaign page and map editor can download a PNG of the latest saved map
image with the unselected territory overlay rasterized on top. Spawn hatching, structure pins, and
faction flags or uploaded logos are included. When logo tinting is enabled, downloaded logos are filled
with the faction color. Download flags are twice the on-map marker size and
structures are three times that size so they remain readable on the PNG. Adjacency arrows are
omitted. If the
map editor has unsaved edits, downloading asks whether to save first; declining downloads the last
saved overlay. The same prompt applies to Download SVG data, which downloads the overlay polygons
and adjacencies as an SVG file. Upload SVG, in the map editor, creates territories from polygon,
polyline, rect, or path data in an SVG file. Exported overlay files restore names, terrain,
structures, ownership, spawns, and adjacencies. Catalog identifiers are remapped onto the target
campaign by exact identifier, then by catalog name; unmatched terrain uses the campaign's first
terrain type, and unmatched structures, owners, and spawns are omitted. Generic SVG files become
new untitled territories using the campaign's first terrain type.

If two endpoints of a new drawing land on the same border of an existing territory, and no other
lines, endpoints, or territories lie between those points on that border, a line matching that
border is inserted when the pointer is released. Releasing the pointer does not close the shape.
Close Territory or Enter closes a valid drawn loop, or tries to enclose a single empty region by
walking touched territory borders or the map image edge. Clicking near the first point also closes a
drawn loop. Extra vertices along a shared border are allowed, including when a neighboring edge
only meets that extra vertex (a T-junction). Traces that sit along a shared
border are allowed; a drawing that covers another territory's interior cannot be closed or saved.

Factions, terrain types, and structures can be expanded or collapsed inside their setup sections.
Each ally group lists its member factions in alphabetical order in a paragraph. Setup also lets a
manager pick those members from a dropdown beside the ally group. When any ally group
exists, unaligned factions are listed after the groups, also alphabetically.

Each faction uses a color flag by default or an uploaded 50×50 flag image. Uploaded logos keep their
original colors unless the manager enables tinting with the faction color. That tint applies everywhere
the logo is shown, including the map and downloaded map PNG.

The Your campaigns and All campaigns pages group campaigns as Active, Upcoming, and Completed.
Each group can be expanded or collapsed. Campaigns start collapsed and expand to view or edit.
Active campaigns are ordered by soonest end. Upcoming campaigns are ordered by soonest start.
Completed campaigns are ordered by most recently finished.

## Forces

- A force has location, controller, faction, supply context, current status, battle history,
  and optional item-objective possession.
- Split forces have independent orders, locations, supply paths, battles, and statuses.
- Two forces belonging to the same player rejoin when they occupy the same territory. The
  surviving force keeps one action slot afterward, and the rejoin is recorded in the play log.
- A force has at most one status: Normal, Diseased, Exhausted, Well Rested, Shaken, or
  Confident, subject to faction exceptions. A no-result forced retreat (neither side submitted)
  does not apply Shaken.
- Neutral territories are unowned land. They are not armies.
- A GM or administrator may inject a ringer battle during an open battle phase. The ringer is
  ephemeral: it is not a `CampaignForce`, does not occupy the map, and leaves no trace win or
  lose. Drought occupation is not applicable. A participating GM may inject against rivals; the
  GM's own player force is never affected, even when the ringer uses the same faction.
- Eligible targets are player forces that are not currently in a battle, including a force that
  is alone or only with allies who are not backstabbing or being backstabbed. Spawn territories
  cannot host a ringer fight. Injection is a logged play command, not a debug correction.
- The GM picks any faction and any catalog mission, or a random mission from that territory's
  suitable pool, and may mark the player as the defender. Same-faction or allied matchups are
  allowed; the fight is forced.
- Ringer supply is that chosen faction's currently owned terrain and operational structures,
  treated as all connected, plus the round's free supply, with no split penalty and no temporary
  pool. Army points use the round escalation row for a solo force (no allied 25 percent). The
  ringer may include mercenaries, not allied extra players. The targeted player uses their real
  supply and army cap. Mission attacker/defender modifiers still apply.
- The ringer scores no campaign points. The targeted player may. Either that player or the
  initiating GM must report; one timely report is authoritative. If neither reports, the ringer
  fight is void: treat it as never happened (no map change, no scoring, no Decision 3 offence).
- Player win: they stay, ownership unchanged, they keep a carried relic, and existing battle
  scoring and statuses apply. Player draw or true battle-point tie: they stay, keep ownership
  and the relic, and receive configured draw scoring. Player loss: the ringer takes no spoils
  and vanishes; the player must retreat as after a normal loss; a carried relic is left on the
  territory with no possessor; a non-spawn territory becomes Neutral. Remaining occupants then
  use the normal claim rules. The ringer's faction and allies gain nothing.

## Delinquency

Removal after inactivity is never automatic. Each force has a campaign-lifetime offence count.
An offence is: no draft at all for a required force after that force exists (split forces each
need their own draft in later windows); no retreat even in draft when a retreat is required; or
a battle where neither side submitted a result. A no-result battle counts once, not also as a
missed retreat. Uncommitted drafts are not offences. One side submitting and the other staying
silent is not a missed-result offence. A forced retreat created by a staff correction is not an
offence. A voided ringer fight (neither report) is not an offence.

Managers are not notified for the first two offences. From the third offence onward, and again
on each later offence, every campaign manager is notified in-app and by email that the player
is a possible kick. The player is not removed unless a manager kicks them. On the campaign page,
staff see a **May be kicked** badge on that player in Participants; the badge opens the matching
campaign-log entry. The log has a Delinquency filter in addition to public chat, private chats,
and the game log.

## Supply

- Normal supply is calculated per force from the owned or allied chain that force can reach.
  Traversal starts at the force's territory when that land is spawn or otherwise in the supply
  network; a force standing off the network still draws from adjacent owned or allied land.
  Each terrain type and each operational owned or allied structure grants configured supply
  points (default 1 for new catalog rows; omitted or invalid values are stored as 0) when the
  force is connected to it. Allied land and structures count as if
  owned for supply and for defense, not for structure campaign-point holdings. Pillaging or
  destroying a structure awards that structure's configured temporary supply points to the
  acting player. Two forces that can both reach the same holdings each have access to that
  chain; map supply is not spent from a shared pool.
- Connected allied territory may participate when alliance rules permit (same ally group,
  not backstabbed).
- Temporary supply is a persistent, consumable **player** pool shown separately as spendable
  supply. The earning player may assign remaining points to any of their forces. Each spent
  point applies to exactly one force: if two forces spend 2 and 1 from a pool of 3, the pool
  is empty. Remaining temporary supply is not added into each force's chain total.
- Split forces each receive the map supply of the chain they can reach after the split-force
  penalty, with a minimum of 1 map supply each. The round's free supply points are granted in
  full to every one of that player's forces. Temporary points are not duplicated. The Hunt in
  Estalia split penalty default is a raw value of 1 and is the application default. The penalty
  may instead be a percentage of map supply (0–100). Catalogs stored before this toggle keep
  the legacy 25 percent when the flag is absent.
- Current chain supply shown under each of the viewer's forces in Summary, on the Participants
  list as the maximum one force can spend from its chain plus remaining spendable supply, and
  on battles to resolve is that force's allowance (map after split penalty, plus round free
  supply) plus remaining temporary supply. Hovering or expanding the amount lists every source:
  connected territories and their terrain, operational structures, allied holdings, special-rule
  bonuses, round free supply, remaining temporary supply, and the split-force penalty. A battle
  to resolve also shows each side's army-point cap for that game
  (round maximum, raised 25 percent per extra allied player then split and rounded up to 10),
  standard supply after the split-force penalty and round free-supply bonus, the controlling
  player's remaining temporary pool, and the round's free-character allowance.
- When a battle result reports supply-costing units, spend is taken first from that force's
  allowance and then from the player's temporary pool.
- Round configuration stores one army-escalation row per round: max army points (10–100000), free
  supply points, and free characters whose base cost does not count against supply. Omitted rows and
  newly added rounds default to 1000/1/1. The Hunt in Estalia preset uses 500/1/1, 750/1/1, 1000/1/1,
  1250/2/1, 1500/2/1, 2000/2/2, 2500/3/2, then 3000/3/2 for round 8 and later. Longer Hunt campaigns
  copy the last row. Changing the round count keeps values already entered for overlapping rounds.
- Faction modifiers are data/rules layered over the base calculation. Hunt keys that add map
  supply without inventing a new structure type are `Slavers`, `SpawningPools`, `GreenTide`, and
  `DefendersOfTheHomeland`.

## Objectives and relics

Objective visibility scopes: Public, Player, Faction, Alliance, Backstabber, and Staff.
Completion and awarded points are separate so a secret objective can be completed without
publicly revealing it.

Named public objectives are a campaign catalog. A manager or administrator awards or revokes
them during play; each change appends a public log fact.

Private objectives are a campaign catalog assigned to a player, a faction, or an ally group.
Unrevealed text and criteria are omitted from unauthorized payloads. The campaign page lists the
viewer's own private objectives at the top of Private objectives and reiterates still-unclaimed
ones in Summary. Other players' claimed or revealed private objectives appear in a collapsed
subpanel ordered by faction name. Unclaimed private objectives for other holders are not listed.
Manual private objectives are claimed by an authorized holder (the player, or any
player in that faction or ally group) who reveals them to a manager. A manager or administrator
approves the claim to reveal it publicly and add its points, or denies it so the holder may
claim again later. Unclaimed manual objectives do not score at campaign end. Automatic private
objectives are scored from live map facts after action resolution: currently controlled
territories, currently controlled or pillaged structures of a configured type, a cumulative
count of destroyed structures of a configured type attributed to the holder's faction, player,
or remaining ally group, finalized battle wins and losses, player-chosen retreats, occupying a
territory that is the same as or directly adjacent to a relic, completed Build or Repair work
of a configured structure type or any type, controlling a relic, defeating a configured opponent
in battle, and force-status facts (gained, caused, or gained after another status). When an
automatic criterion is met, the objective is revealed, its
points are added, and the public log records that the holder scored. Destroyed structures are
removed from the map, so destroy criteria use append-only destruction facts rather than current
holdings. Build and Repair criteria use append-only work facts. Force-status criteria use
append-only status-change facts. Hidden relics still score adjacency and control on the server.
A random DefeatOpponent target is chosen when the assignment is created and excludes the holder's
own player, faction, or remaining ally group. Same-territory occupancy counts as adjacent to a
relic; other territories must share a direct overlay connection.

Item objectives are named catalog items (none, one, or many). Launch placement is Random or
Placed. Hidden-until-found items are omitted from player play payloads, including location and
possessor, until found or until staff in an active debug session clicks Reveal hidden
objectives. Staff in that debug session may see still-hidden items. Once revealed, an item
stays revealed. A force that Moves or Retreats drops a carried item on the territory it left;
another force that is alone in that territory and not in battle picks it up. A battle winner
takes items held by participants or lying in the battle territory; a draw does not transfer
them. Items may occupy a spawn territory only when that catalog flag is enabled (off by
default). The possessing player may resolve one configured choice on a held item. That choice
applies its only result, or one result picked at random when several are configured. A destroyed
item is gone: it awards no points, cannot be dropped, picked up, or taken as spoils, and is
omitted from standings. A replacement item, when configured, appears with the possessor or on
the same territory and uses that catalog type's own flavor, choices, and special rules.

## Corrections

A GM or administrator enters campaign debug mode from the campaign page. Entering debug, each
correction, and exiting debug are public log facts. Original orders, results, and audit events are
never overwritten. While the current action window is open, a debug correction saves a staff draft
without revealing the secret action in the log. After that window has resolved, the previous action
can be re-resolved only while the following phase is still open, by restoring the captured
pre-resolution snapshot and appending a staff correction. If that following phase ends before
the GM commits the override, the pending change is void. After the campaign has completed, staff
have a grace window equal to the next template phase that would have occurred (for example, after
a final battle phase, the first action length of a hypothetical extra round). If that grace
elapses with no committed override, the completed state is locked.

Re-resolution uses the corrected previous window and then reapplies other players' actions from
that snapshot. Players whose force location, battle state, occupied or targeted territory, or
current-phase order legality changed are notified in-app and by email. Their committed current-phase
orders return to draft when still legal, or are nullified when no longer legal so they must enter a
new order. Standings-only changes do not uncommit anyone. If a battle-result override requires a
retreat, that retreat is assigned automatically to spawn (same as a missing retreat) and
does not increment delinquency. Keep a current-phase battle report when the same participants still
fight in the same territory; otherwise keep it in history but do not apply it to a new engagement.

Manager battle-result overrides also
require the active debug session. Concurrent debug sessions are not allowed; any manager or
administrator may exit the current session.

A GM reopening or correcting a prior state never mutates history in place. It creates a new
campaign revision, identifies downstream state requiring recomputation/review, and notifies
affected users in-app and by email. Concurrent corrections must fail safely rather than use
last-write-wins.

## Play log

The campaign page shows a collapsible, scrollable log at full page width near the top
for upcoming, in-progress, and completed campaigns. The log loads independently of the rest of
the campaign page, the same way All Campaigns loads public site chat separately from the campaign
list: chat can appear while campaign metadata is still loading, and the reverse. Each entry is
formatted as
`originator: text` followed by a timestamp. Recent entries (under 24 hours) use a relative label
with the absolute time in the `title` attribute; older entries show the absolute time. On small
viewports the timestamp sits on a secondary line and the log uses the body font.
Campaign-generated facts use the originator name `Campaign` and always belong to the public channel.
Member chat uses the author's display name
snapshotted when the message was posted. Chat originators and `@` mentions of current members
link to that player's public profile. The log refreshes while the page is open. Sending chat
is not a form save: it does not show the saving overlay or the success banner. Failed sends show
an error on the log.

Members compose to Everyone (public), another current member (direct), a faction, or an ally
group. The compose recipient is a typable field with mouse and keyboard autocomplete, including
Everyone and member usernames. Private messages are stored on the play log with audience metadata and are filtered on
read. They are returned only to the sender and the selected audience. Campaign managers do not
receive other members' private chats. A system administrator may inspect all private chats only
while they are the active debug actor on that campaign. Private chat never appears in exports.
It appears in the visible log only when the viewer enables the private-chat filter for themselves.

Independent filters show public chat, private chats the viewer is allowed to see, and/or the
game log. Game-log facts always go to the public channel. A campaign manager or administrator
may download public chat and/or game-log facts as one text or CSV file at any time, including
before launch, during play, and after the campaign ends. That file is the same payload a later
outbound sender would use. Private chats are omitted from the download even when the caller can
see them on screen. The log records campaign start, campaign end with final scores and remaining revealed item
objectives (a later manager score or item adjustment appends an updated final snapshot),
manager extensions of remaining phases or rounds, resolved
actions after an action window closes (including Hold for every force), attempted actions that
were invalid or conflicted and became Hold, battles created or finalized, manager battle-result
overrides, debug enter/exit and debug order corrections, player retreats, automatic force rejoins when the same player's forces occupy one
territory, and automatic substitutions: missing orders become Hold, deadline-submitted drafts,
missing retreats assigned to spawn, no-result forced retreats, ringer battles (including
voided neither-report fights), and delinquency notices from the third offence onward.
Unresolved secret orders, including drafts and unrevealed commitments, are never written to or
returned in the log. A player may uncommit a committed draft only while the action window is
still open; after the window closes, orders resolve and cannot be returned to draft.

Current members may post chat in this log, including before launch. Chat and
`@` tags are limited to people who currently belong to the campaign. An unescaped `@` followed
by a member's username, or by a display name that uniquely identifies one member, is a tag.
`\@` is a literal `@`. Email-like text such as `ada@example.test` is not a tag. People who have
not joined cannot chat, cannot be tagged, and are omitted from mention autocomplete. Leaving
removes the ability to chat or be tagged; earlier messages keep the display name recorded at
post time. Mentions notify only tagged members who can see the message. Public chat without a
mention does not notify every member. Site-wide chat is never written to this log.

## Site chat

The All Campaigns page shows a collapsible public site chat above the campaign lists. Messages
are stored separately from campaign play logs and never appear in a campaign. Sending is not a
form save: it does not show the saving overlay or the success banner. Failed sends show an error
on the chat box. The board refreshes while the page is open.

Every signed-in user may post except seeded test accounts. Player messages are public. `@` tags
may name any non-test account on the site; unknown usernames are rejected with "You can only tag
people who have an account on this site." `\@` is a literal `@`. Email-like text is not a tag.
Mentions notify only tagged people who can see the message. Chat originators and mentions link
to public profiles. Test accounts are omitted from mention autocomplete and cannot post.

A user may block another person. Blocking is stored one-way and hides player messages both ways:
neither person sees the other's player chat, and mentions of a blocked person do not notify them.
The block list can be toggled from a message or from the blocked-people list. Administrator
announcements remain visible through blocks.

Administrators may send an administrator message to everyone or to one person. Those messages
are still public and show an `Admin` or `Admin to {name}` badge. Everyone, or that one person,
is notified in-app and by email. Notification and email bodies omit the chat text and point to
`/campaigns/all`.

Messages are rejected when they contain prohibited language. Each message has a language flag
used only for filtering, not translation. Supported flags are English, Spanish, French, German,
Dutch, Italian, Russian, Korean, Chinese, Japanese, Danish, Swedish, Norwegian, Finnish, Hindi,
and Arabic. New messages default to English unless the composer picks another flag. The viewer
may hide languages; by default every language is visible. Compose language, language filter
checkboxes, and the block list sit in a collapsed subpanel below Send. Compose language and
language filters are stored in a `siteChat` cookie (`Path=/`, Max-Age one year, SameSite=Lax). A
user may also set a default compose language on their profile; that value is used until they
change language on All Campaigns.

## Notifications

Users may enable in-app notices, email notices, or both on their profile. Stored notices cover
mentions, private chats, campaign start, campaign end, a new phase after the previous window
resolves, being removed from a campaign, public site-chat mentions, and administrator site-chat
announcements. Live attention items always appear when the user still needs to choose a faction,
commit orders, submit a battle result, or record a retreat. From the third delinquency offence
onward, campaign managers are notified that a player is a possible kick. Email copies never include hidden
orders, relics, private chat text, or site-chat bodies; they tell the recipient to sign in and
open the campaign or All Campaigns. Seeded test accounts never receive email. The home page lists
campaigns that need attention, then notifications (five per page, dismissable, with dismiss all),
then site news. When none remain, it shows "No new notifications."
Profile editing and the public profile live on their own pages. The profile includes a default
site-chat compose language and a date-and-time display format.

## News

Administrators publish site-wide news as markdown articles. The home news board shows the two
newest articles, then pages of two, newest first, with a scrollbar when an article is long. Markdown is HTML-encoded and
then a conservative subset is rendered; user-provided HTML is not executed.
