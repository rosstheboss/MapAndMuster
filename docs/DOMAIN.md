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
according to that preference. Email, created/updated timestamps, time-zone preference, and the
legal name when the owner chose username display are omitted from public queries. Created and
last-edited times are visible only to the owning user. Light or dark appearance is a client
preference stored in a cookie so it remains after sign-out; light mode is the default.

## Campaign setup

A campaign has a name (3-80 characters), optional description (500 characters), player-slot
count (2-100), optional location (city, state or province, and country; all optional, but a city
requires a state or province and a state or province requires a country), public or private join
visibility, a publicly-viewable flag (on by default), optional labeled external links (at most 20
http/https URLs), a raster map image, and at least two factions. Each faction has a unique
color and may have subfactions. A faction may require players who choose it to pick a
subfaction; that flag may only be enabled when at least one subfaction is listed. Optional
ally groups may include two or more factions; every faction cannot belong to a single ally
group.

Setup may apply a faction preset. Applying a preset replaces the current faction and subfaction
list with an alphabetically sorted copy of that catalog entry, including colors and whether a
subfaction is required. Later add/remove/rename edits apply only to that campaign and do not
change the preset. The initial catalog includes Warhammer: The Old World. In that preset,
Daemons of Chaos includes the subfactions Khorne, Nurgle, Slaanesh, and Tzeentch (alphabetical)
and requires a subfaction choice. Setup can also clear the faction list (back to two empty
slots) or clear all ally groups.

The creating user is always a campaign manager (Game Master). If they also participate, they
occupy one player slot. Private campaigns store a hashed join password; the plaintext password
is never returned. Publicly viewable campaigns may be opened by any signed-in user. When a
campaign is not publicly viewable, only players, managers, and administrators may open it after
it starts. Upcoming campaigns still appear on All Campaigns so players can join. Campaign names
and faction names reject the same prohibited-language terms as usernames.

Your Campaigns lists campaigns the user manages or plays in. All Campaigns lists upcoming
campaigns plus publicly viewable active and completed campaigns, using the same grouping and
sort: active by soonest end, upcoming by soonest start, completed by latest end. Listings show
player slots occupied of maximum, name, description, filled location parts, proposed start and
end, and for active campaigns the current round, phase label (Action 1, Action 2, Battle, or
Battle N when a round has more than one battle), and a countdown until the current phase ends.

A signed-in user may join an upcoming campaign that still has an open player slot. Public
campaigns join without a password; private campaigns require the join password. Members who are
not managers may leave. Managers edit instead of joining. Players open an in-progress campaign
with Play. Upcoming and completed campaigns, and campaigns the viewer is not playing, use View.

Only a manager may edit or delete a campaign. Deletion removes the campaign from every member's
list. A raster map image is required when creating a campaign; SVG and other active content are
rejected. Maps may be JPEG, PNG, or WebP up to 20 MB. One map may later be replaced; the previous
map file is deleted when it is no longer used. Deleting a campaign also deletes its stored map and
user-uploaded catalog images. Built-in structure icons are application assets and are never deleted.

Setup sections (details, schedule, visibility, ally groups, factions, subfactions, terrain,
structures, missions, links, and map) can be expanded or collapsed. Section actions collapse
with their section. Invalid sections expand automatically when save validation fails. Sections
start expanded. Setup keeps Back to campaigns, Expand All, Collapse All, and Save or Create in a
sticky toolbar. Edit campaign also includes Edit map, which opens the map editor without saving
the current form.

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

### Map overlay editor

After campaign creation, the creating manager is taken to the map editor. Territories are drawn as
an overlay on the rectangular raster map; the image itself is not modified. Overlay coordinates are
normalized to the unit square. Drawing stays inside the image rectangle. Territories may share a
border but their interiors must not overlap. The drawing cursor highlights when it is about to snap
to an existing vertex. Managers may undo or erase segments, assign an optional unique name and
description (otherwise the display number 1, 2, 3… is used), select a required terrain type,
select at most one optional structure, assign optional ownership (otherwise Neutral), assign an
optional spawn faction (at most one spawn per faction), and apply a transparent overlay color.

Generate Connections suggests adjacency arrows from shared borders. User-created (manual) arrows
are kept on regenerate, and those pairs are skipped. Generated arrows may be replaced. Managers may
add or delete arrows, including generated ones, and may clear all arrows. Arrow markers are editor
aids and are not part of the published map image. Hovering an arrow enlarges it by half of its on-screen
size and highlights both connected territories.

Campaign setup owns the terrain-type and structure catalogs. The initial terrain types,
alphabetically, are Beach, Cave, Desert, Forest, Highlands, Jungle, Lake, Mountain, Plains, Riverlands, Sea,
and Swamp.
Each has a unique color, a symbol, and at least one mission. Setup starts each terrain type with one
empty mission row. The initial structures, alphabetically, are Capital City, Castle, City, Fortification, Supply
Depot, and Town. Each structure uses either a built-in icon or an uploaded logo image, not both.
Clearing or replacing an uploaded logo deletes only that uploaded file. Built-in icons remain in the
application.
Uploaded structure logos are limited to 50×50 pixels; larger images are shrunk to that size.
Structures start with no missions. Structures may have zero or more missions. A territory uses its structure
missions when that structure has any; otherwise it uses its terrain missions. Mission attachments are an http/https
URL or a stored PDF/Word file, not both. Mission names are unique across the campaign. An already
configured mission may be selected again for another terrain or structure instead of uploading a
duplicate file. New uploads and reused missions may be mixed.

Hovering or selecting a territory, while editing or viewing, shows Name, Description, structures,
ownership (or Neutral), spawn-location faction, adjacent territories, terrain, and missions.
In Select mode, a mouse click on a territory keeps that territory selected. Ctrl+click (or Command+click)
adds or removes territories from the selection. A click on the map with no territory clears the
selection. Selected territories can be deleted with Delete or the Delete territory control, including
when several are selected. Dragging a selection moves those territories together when the move stays on
the map and does not overlap another territory; shared edges are allowed.
Owned territories also show a flag at up to 50×50 pixels, using the faction color flag by default or an
uploaded image that is not recolored. Structure logos and ownership flags sit at 50×50 pixels in the
territory center when that size fits; otherwise they shrink and shift to stay inside the polygon, as centered
as possible. When a structure is present, the ownership flag sits beside it. Highlights use a subtle
contrasting glow. Selected or hovered territories use the full selected glow. Connected adjacent territories
glow with half of the usual fill transparency. When several territories are selected, those
territories stay selected and every other connected territory uses the half-glow. When a territory
has an overlay color, that color is the glow, strongest around the territory border. Overlay
colors may be assigned randomly, removed, taken from terrain, or set manually.

The map overlay starts fitted to the panel. Zoom is 10% to 800% of the map image's actual pixel size,
in 10% steps, with a typed percent field, a 100% control, and a Fit to panel control. 100% shows the
image at its native size and centers it. Fit to panel scales the image to the view and recenters it.
Drawing coordinates stay normalized to the full-size image. Snap distance, minimum draw spacing, and
overlay stroke widths are measured in screen pixels so zooming in lets a manager trace fine coasts and
province borders. When the zoomed image is larger than the panel, it can be panned
but not dragged past the image bounds. Drawing territories requires a pointer; other map controls,
including zoom, pan, tool choice, and the territory list, are keyboard accessible.
A click on empty map, including the letterboxed panel around a fitted image, clears the territory
selection. Hovered adjacency arrows stay a modest on-screen size and enlarge by half relative to that
resting size; they do not scale with the map image.

Edit campaign and the campaign page show a static 200×113 map preview of the current image. It is not
zoomable or selectable. The campaign page and map editor can download a PNG of the latest saved map
image with the unselected territory overlay rasterized on top. Adjacency arrows are omitted. If the
map editor has unsaved edits, downloading asks whether to save first; declining downloads the last
saved overlay.

If two endpoints of a new drawing land on the same border of an existing territory, and no other
lines, endpoints, or territories lie between those points on that border, a line matching that
border is inserted when the pointer is released. If a drawn line’s endpoints touch one or more
territory borders and walking those borders can close a single empty region, that loop is enclosed
as a new territory. If both endpoints sit on the map image edge, the shape is enclosed along that
image edge.

Factions, terrain types, and structures can be expanded or collapsed inside their setup sections.
Each ally group lists its member factions in a paragraph. When any ally group exists, unaligned
factions are listed after the groups.

Each faction uses a color flag by default or an uploaded 50×50 flag image. Uploaded flags are not
recolored.

The Your campaigns and All campaigns pages group campaigns as Active, Upcoming, and Completed.
Each group can be expanded or collapsed. Campaigns start collapsed and expand to view or edit.
Active campaigns are ordered by soonest end. Upcoming campaigns are ordered by soonest start.
Completed campaigns are ordered by most recently finished.

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
