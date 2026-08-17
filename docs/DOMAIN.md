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
Created and last-edited instants are stored in UTC. The owner chooses a time zone for display;
when none has been stored yet, those times are shown in UTC.

Other users may see username, location, avatar, and either the username or the full name
according to that preference. Email, created/updated timestamps, time-zone preference, and the
legal name when the owner chose username display are omitted from public queries. Created and
last-edited times are visible only to the owning user. Light or dark appearance is a client
preference stored in a cookie so it remains after sign-out; light mode is the default.

A public profile also lists campaigns the viewer may open: publicly viewable campaigns plus
private campaigns the viewer shares with that player. Scores and rankings are not shown until
those rules are implemented. Display names in chat, mentions, the Participants panel, and other
member lists link to that public profile. Profiles opened that way include a Back control that
returns to the previous in-app screen.

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

Setup may apply a standard terrain preset or a standard structures preset the same way: applying
the preset replaces the current list with a copy of the current catalog values. Later catalog
edits in code update those presets, the same as faction presets. Setup may also apply a whole
campaign preset. The initial catalog includes The Hunt in Estalia, which copies the Old World
factions, standard terrain, standard structures, and that campaign's item-objective list (empty
until named items are added). Applying a campaign preset fills the campaign name only when the
name field is empty.

Optional item objectives may be none, one, or many (at most 50), each with a unique name.
Defaults are hidden until found, randomly placed, and not allowed on a spawn territory. A
Placed item is assigned to a territory in the map editor. Hidden items stay off player views
until a force finds them or a manager or administrator in debug mode clicks Reveal hidden
objectives. Found or staff-revealed items stay revealed.

The creating user is always a campaign manager (Game Master). If they also participate, they
occupy one player slot. Private campaigns store a hashed join password; the plaintext password
is never returned. Publicly viewable campaigns may be opened by any signed-in user. When a
campaign is not publicly viewable, only players, managers, and administrators may open it after
it starts. Upcoming campaigns still appear on All Campaigns so players can join. Campaign names
and faction names reject the same prohibited-language terms as usernames.

The campaign page lists attached members in a Participants panel: each player's display name
(linked to their public profile), selected faction and subfaction when chosen, and roles
Manager, Player, and/or Admin when those apply.

Your Campaigns lists campaigns the user manages or plays in. All Campaigns lists upcoming
campaigns plus publicly viewable active and completed campaigns, using the same grouping and
sort: active by soonest end, upcoming by soonest start, completed by latest end. Listings show
player slots occupied of maximum, name, description, filled location parts, proposed start and
end, and for active campaigns the current round, phase label (Action 1, Action 2, Battle, or
Battle N when a round has more than one battle), and a countdown until the current phase ends.

A signed-in user may join an upcoming campaign that still has an open player slot. Public
campaigns join without a password; private campaigns require the join password. Members who are
not managers may leave. Managers edit instead of joining. Listings open the campaign page with
Open. There is one campaign page: a member's role and involvement decide which controls appear.
Players submit orders and battle results there. Managers also see schedule extension, and
managers or administrators can enter a logged debug session to correct orders, re-resolve the
previous action while the following phase is still open, or override battle results. Upcoming
and completed campaigns use the same page without live order controls. The campaign page includes
a collapsible public log and member chat for upcoming, in-progress, and completed campaigns.
Your Campaigns also offers Duplicate campaign on every listed campaign. Duplication copies the map overlay, factions, missions, ally
groups, links, visibility, location, and schedule template into a new campaign whose start is one
week after the duplication instant in the campaign time zone. The duplicating user becomes the
manager of the copy and occupies a player slot only when they were a player on the source.
Raster maps, flags, structure logos, and mission files are reused until the copy replaces them;
the overlay SVG data is copied. Play state, memberships, and unresolved orders are not copied.

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

The campaign page keeps Back to campaigns, Expand All, Collapse All, and (when allowed) Debug,
Edit campaign, and Edit map in a sticky toolbar. Every panel except the status strip can be
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

1. `Scheduled`: before the start instant.
2. `InProgress`: inside a configured round and phase. The current round number, phase, and
   phase window are included on the campaign page.
3. `Completed`: at or after the computed end instant.

A phase boundary belongs to the following phase. After launch, actual phase windows are stored
and may diverge from the original template when a window closes early or a manager extends it.

After the start instant, managers cannot change the map, the ordered action and battle steps,
name, description, factions, faction abilities, ally groups, terrain, structures, missions, or
most other setup. They may increase the number of rounds, not below the current round and not
above 52. They may lengthen the current round by adding time to the current or remaining action
and battle windows; a window cannot be shortened below the duration already in effect for that
window. Added rounds use the original phase template and make the campaign longer.

Unused time from an action or battle window that closes early is added to the next window.
Simultaneous-action resolution runs immediately when an action window closes; any wall-clock
pause before the next window opens is taken from that next window.

## Role and actor model

Administrator permissions include GM and player capabilities. Campaign GMs include player
capabilities and may simultaneously have a Player membership. Multiple GMs may exist.

When staff act for another party, record:

- actual actor: staff user performing the command;
- effective actor: player or neutral force represented;
- reason, timestamp, before/after values, revision, and notifications.

## Action-window lifecycle

1. `Open`: required participants save a draft for every force, then commit or uncommit.
2. `Closing`: a transaction freezes the required participant/order set.
3. `Revealed`: submitted/default orders become visible according to policy.
4. `Resolving`: deterministic precedence and conflict rules run.
5. `Resolved`: resulting map/battle state is committed once.
6. `Reopened`: staff correction creates a new revision and a new controlled editing window.

The final required commitment closes an open window atomically. A player may commit only after every required force has a saved draft. Before that instant, a player
may uncommit a committed order back to draft. At the deadline, the latest valid draft is
submitted. Missing slots become `Hold`. After the window closes, orders resolve and cannot be
returned to draft. Each force requires an action; same-player forces that occupy one territory
rejoin into one surviving force and therefore one later action. Only users/forces that owe an
order participate in the early-close calculation.

Players pick a faction, and a required subfaction when the faction demands one, before they
receive a starting force. On the campaign page, every player (including a manager who occupies
a player slot) must choose that faction before they can play. A participant who has not chosen a
faction may do so until they have one and cannot submit orders for a round until they do. A player
may change their chosen faction until the campaign starts. After the campaign has started, a chosen
faction cannot be changed. Each player force starts at that faction's spawn territory
when the campaign launches or when they join play later; subfactions use the same spawn.

Orders resolve simultaneously against the window's starting map state. Processing order is
movement and splits, then backstab alliance breaks, then battles from enemy co-location, then
`Build`, `Pillage`, and `Repair` for forces that are not in battle. An invalid `Move`, `Split`,
`Build`, `Pillage`, `Repair`, or `Backstab` becomes `Hold`. A force may not enter or claim
another faction's spawn. After movement, enemy forces that occupy the same territory create a
battle; later action slots for those forces become `Battle`. Same-player forces that share a
territory rejoin. Uncontested occupation by a single faction claims the territory and plants
that faction's flag, except that a spawn always keeps its faction's flag. Collisions that still
lack a documented ranking, including competing `Build`, `Pillage`, or `Repair` actions on the
same territory and competing arrivals, become `Hold` rather than an invented winner.

## Initial action vocabulary

Player-submittable actions in an open action window are listed in this order:

- `Hold`: remain and receive applicable resting effects.
- `Move`: travel to an allowed adjacent territory; invalid move becomes Hold.
- `Build`: create an allowed structure in a non-spawn territory that has no intact structure.
  Only structure types flagged buildable may be chosen. Town, Capital City, City, and Castle
  start not buildable; Supply Depot and Fortification start buildable.
- `Pillage`: progress an enemy or unowned intact structure that is flagged pillageable from
  operational to pillaged. A second Pillage against a pillaged structure that is flagged
  destructible removes it from the map. Capital City starts not pillageable. Capital City, City,
  and Castle start not destructible. A force cannot pillage a structure it already owns.
- `Repair`: restore a pillaged structure the force's faction owns.
- `Split`: create a second force in an eligible adjacent territory; maximum two per player in
  the supplied rules.
- `Backstab`: terminate an alliance relationship. If the force shares a territory with a former
  ally after resolution, a battle is created.

Battle-phase and system actions:

- `Retreat`: move a losing/withdrawing force to an eligible territory or spawn fallback. Players
  submit retreat after a battle, not during an action window.
- `Battle`: automatic system action created by resolution; players do not submit it directly.

Battle overrides incompatible orders. If Action 1 puts a force in battle, later action slots for
that force become Battle.

## Battle lifecycle

`Pending -> AwaitingResults -> Finalized | Disputed -> GMResolved`

- Each participant may submit one current result; revisions retain history.
- Staff may submit on a participant's behalf with actual/effective actor attribution.
- Equivalent submissions finalize automatically.
- A participant may accept the opponent's current submission; that counts as an equivalent
  submission.
- One timely submission becomes authoritative at the deadline.
- Conflicting submissions become Disputed and notify GMs in-app and by email.
- GM resolution preserves both submissions and appends an authoritative result.
- Three-or-more-participant engagements require a configured mission/result schema.
- A battle phase ends early when every engagement is finalized and every required retreat is
  recorded, and also when no battles remain for anyone to report. Unused time is added to the
  next window. At the deadline, a missing retreat uses
  the spawn fallback. A battle with no submissions at the deadline stays open for GM resolution
  until decision 2 in `docs/DECISIONS-NEEDED.md` is recorded.

## Territory and structures

- Adjacency is a graph edge, not an assumption based only on touching pixels.
- Spawn locations prohibit enemy entry, battle, construction, and capture. The spawn faction's
  flag is always present there.
- A faction that controls a non-spawn territory displays its flag there.
- At most one structure occupies a territory under the supplied rules.
- Structure type, owner/controller, and condition are separate concepts.
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
- Each structure type has a built-in operational icon and a built-in pillaged icon. Campaign
  setup may replace either with a user-uploaded 50×50 logo.

### Map overlay editor

After campaign creation, the creating manager is taken to the map editor. Territories are drawn as
an overlay on the rectangular raster map; the image itself is not modified. Overlay coordinates are
normalized to the unit square. Drawing stays inside the image rectangle. Territories may share a
border but their interiors must not overlap. The drawing cursor highlights when it is about to snap
to an existing vertex. Managers may undo, redo, or erase segments, assign an optional unique name and
description (otherwise the display number 1, 2, 3… is used), select a required terrain type,
select at most one optional structure and whether that structure starts Operational or Pillaged,
assign optional ownership (otherwise Neutral), assign an optional spawn faction (at most one
spawn per faction), place catalog item objectives that use Placed launch placement, and apply a
transparent overlay color.

Auto Generate Connections suggests adjacency arrows from shared borders. User-created (manual) arrows
are kept on regenerate, and those pairs are skipped. Generated arrows may be replaced. Managers may
add or delete arrows, including generated ones, and may clear all arrows. A pair of territories has
at most one connection, and every connection is between exactly two territories. Select two
territories and click Connect, or use the Connect tool and click two territories. Connection arrows
are selectable only in Select, where clicking an arrow shows both territories in the side panel so
the pair can be changed or deleted, and in Erase, where clicking an arrow deletes it. Drawing and
Connect ignore arrows so they do not intercept the pointer or affect overlay drawing. Arrow markers
are editor aids and are not part of the published map image. Each connection arrow stretches across
the shortest gap between its territories so adjacent arrows do not cross. The arrow head and up to
10 pixels of the shaft overhang each territory so the heads stay visible and selectable in Select
and Erase. Hovering an arrow in those tools glows that arrow, grows it by half of its resting size,
and glows both connected territories without washing the rest of the map.

Campaign setup owns the terrain-type and structure catalogs. The initial terrain types,
alphabetically, are Beach, Cave, Desert, Forest, Highlands, Jungle, Lake, Mountain, Plains, Riverlands, Sea,
and Swamp.
Each has a unique color, a symbol, and at least one mission. Setup starts each terrain type with one
empty mission row. The initial structures, alphabetically, are Capital City, Castle, City, Fortification, Supply
Depot, and Town. Town, Capital City, City, and Castle are not buildable; Supply Depot and
Fortification are. Capital City is not pillageable. Capital City, City, and Castle are not
destructible. Each structure uses either a built-in icon or an uploaded logo image, not both.
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
adds or removes territories from the selection. Dragging on empty map draws a selection box that
selects every territory it intersects; the box is 50% transparent gray with a black 2px dashed
border and disappears when the button is released. A click on the map with no territory clears the
selection. When two or more territories are selected, the side panel lists all of them and the map
highlights each as selected. Selected territories can be deleted with Delete or the Delete territory
control, including when several are selected. Dragging a selection moves those territories together
and keeps them on the map. Shared borders are allowed; a newly drawn or moved border must not
overhang into another territory's interior. While a group is being dragged, it is highlighted green
at about 70% fill with a centered checkmark of at most 50×50 pixels when it can be dropped, or red
at about 70% fill with a centered X of at most 50×50 pixels when it cannot. Dropping while red
restores the group to the position it had when the drag started.
Owned territories also show a flag at up to 50×50 pixels, using the faction color flag by default or an
uploaded image that is not recolored. Structure logos and ownership flags sit at 50×50 pixels in the
territory center when that size fits; otherwise they shrink and shift to stay inside the polygon, as centered
as possible. When a structure is present, the ownership flag sits beside it. Highlights use a subtle
contrasting glow. Selected territories use a full highlight: about 70% fill and double the usual
border thickness. Hovered territories, possible action destinations, and territories connected to
the current selection use a half highlight: 50% fill and 1.5 times the usual border thickness.
When a territory qualifies for both, the full highlight wins. When several territories are selected,
those territories stay fully highlighted and every other connected territory uses the half highlight.
When a territory
has an overlay color, that color is the glow, strongest around the territory border. Overlay color
mode is Random Colors, Color By Terrain, or Manual Colors. Switching to Random Colors or Color By
Terrain recolors every territory. A new territory, or a terrain change while Color By Terrain is on,
uses that mode's color. Remove Colors switches to Manual Colors and clears every overlay color.

The map overlay starts fitted to the panel. Map panels are full width in their parent. Zoom
controls sit across the top of the map in this order: zoom percent field, +, -, Fit, and 100%.
Zoom is 10% to 800% of the map image's actual pixel size, in 10% steps. 100% shows the
image at its native size and centers it. Fit scales the image to the view and recenters it.
Drawing coordinates stay normalized to the full-size image. Snap distance, minimum draw spacing, and
overlay stroke widths are measured in screen pixels so zooming in lets a manager trace fine coasts and
province borders. When the zoomed image is larger than the panel, it can be panned
but not dragged past the image bounds. Hold a right-click (context-click) or middle-click and drag to
pan without drawing, erasing, or selecting; the mouse wheel still zooms the same way it does with any
tool. Left-click drag does not pan. Arrow keys and space-drag also pan. Drawing territories requires
a pointer; other map controls, including zoom, pan, tool choice, and the territory list, are keyboard
accessible. A click on empty map, including the letterboxed panel around a fitted image, clears the
territory selection. Only the hovered or selected arrow glows. Saving the map graph does not change
zoom, pan, or fit. Save Map is disabled until there are unsaved overlay edits. After a successful
save, the green banner "Successfully saved changes." is followed by the last-saved time. Clear
Unsaved Changes is disabled until there are unsaved overlay edits; it discards those edits and
restores the last saved graph without resetting zoom or pan.

Edit campaign shows a static 200×113 map preview of the current image. It is not
zoomable or selectable. The campaign page shows the interactive map at full width, with territory
details under the map. The campaign page and map editor can download a PNG of the latest saved map
image with the unselected territory overlay rasterized on top. Adjacency arrows are omitted. If the
map editor has unsaved edits, downloading asks whether to save first; declining downloads the last
saved overlay. The same prompt applies to Download SVG data, which downloads the overlay polygons
and adjacencies as an SVG file. Upload SVG, in the map editor, creates territories from polygon,
polyline, rect, or path data in an SVG file. Exported overlay files restore names, terrain,
structures, ownership, spawns, and adjacencies. Generic SVG files become new untitled territories
using the campaign's first terrain type.

If two endpoints of a new drawing land on the same border of an existing territory, and no other
lines, endpoints, or territories lie between those points on that border, a line matching that
border is inserted when the pointer is released. Releasing the pointer does not close the shape.
Close Territory or Enter closes a valid drawn loop, or tries to enclose a single empty region by
walking touched territory borders or the map image edge. Clicking near the first point also closes a
drawn loop. Extra vertices along a shared border are allowed. A newly drawn border must not overhang
into another territory's interior.

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
  and optional item-objective possession.
- Split forces have independent orders, locations, supply paths, battles, and statuses.
- Two forces belonging to the same player rejoin when they occupy the same territory. The
  surviving force keeps one action slot afterward, and the rejoin is recorded in the play log.
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

Item objectives are named catalog items (none, one, or many). Launch placement is Random or
Placed. Hidden-until-found items are omitted from player play payloads, including location and
possessor, until found or until staff in an active debug session clicks Reveal hidden
objectives. Staff in that debug session may see still-hidden items. Once revealed, an item
stays revealed. A force that Moves or Retreats drops a carried item on the territory it left;
another force that is alone in that territory and not in battle picks it up. A battle winner
takes items held by participants or lying in the battle territory; a draw does not transfer
them. Items may occupy a spawn territory only when that catalog flag is enabled (off by
default). Relic choice options and effects remain in `docs/DECISIONS-NEEDED.md`.

## Corrections

A GM or administrator enters campaign debug mode from the campaign page. Entering debug, each
correction, and exiting debug are public log facts. Original orders, results, and audit events are
never overwritten. While the current action window is open, a debug correction saves a staff draft
without revealing the secret action in the log. After that window has resolved, the previous action
can be re-resolved only while the following phase is still open, by restoring the captured
pre-resolution snapshot and appending a staff correction. Manager battle-result overrides also
require the active debug session. Concurrent debug sessions are not allowed; any manager or
administrator may exit the current session. Downstream invalidation of later rounds remains in
`docs/DECISIONS-NEEDED.md`.

A GM reopening or correcting a prior state never mutates history in place. It creates a new
campaign revision, identifies downstream state requiring recomputation/review, and notifies
affected users in-app and by email. Concurrent corrections must fail safely rather than use
last-write-wins.

## Play log

The campaign page shows a collapsible, scrollable log at full page width near the top
for upcoming, in-progress, and completed campaigns. Each entry is formatted as
`(local-timestamp) originator: text`. Campaign-generated facts use the originator name
`Campaign` and always belong to the public channel. Member chat uses the author's display name
snapshotted when the message was posted. Chat originators and `@` mentions of current members
link to that player's public profile. The log refreshes while the page is open. Sending chat
is not a form save: it does not show the saving overlay or the success banner. Failed sends show
an error on the log.

Members compose to Everyone (public), another current member (direct), a faction, or an ally
group. The compose recipient is a typable field with mouse and keyboard autocomplete, including
Everyone and member usernames. Private messages are stored on the play log with audience metadata and are filtered on
read. They are returned only to the sender and the selected audience. Campaign managers do not
receive other members' private chats. A system administrator may inspect all private chats only
while they are the active debug actor on that campaign. Private chat never appears in exports or
the visible log unless the viewer enables the private-chat filter for themselves.

Independent filters show public chat, private chats the viewer is allowed to see, and/or the
game log. Game-log facts always go to the public channel. The log records campaign start,
manager extensions of remaining phases or rounds, resolved
actions after an action window closes (including Hold for every force), attempted actions that
were invalid or conflicted and became Hold, battles created or finalized, manager battle-result
overrides, debug enter/exit and debug order corrections, player retreats, automatic force rejoins when the same player's forces occupy one
territory, and automatic substitutions: missing orders become Hold, deadline-submitted drafts,
missing retreats using the spawn fallback, and battles held open when resolution cannot finish.
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
mention does not notify every member.

## Notifications

Users may enable in-app notices, email notices, or both on their profile. Stored notices cover
mentions, private chats, campaign start, campaign end, and a new phase after the previous window
resolves. Live attention items always appear when the user still needs to choose a faction,
commit orders, submit a battle result, or record a retreat. Email copies never include hidden
orders, relics, or private chat text; they tell the recipient to sign in and open the campaign.
The home page lists items that need attention, then site news. When none remain, it shows
"No new notifications." Profile editing and the public profile live on their own pages.

## News

Administrators publish site-wide news as markdown articles. The home news board shows one article
per page, newest first, with a scrollbar when an article is long. Markdown is HTML-encoded and
then a conservative subset is rendered; user-provided HTML is not executed.
