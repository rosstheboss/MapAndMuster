# ADR 0004: Server-Sent Events for Campaign Updates

- Status: Accepted
- Date: 2026-09-11

## Context

The Angular client polled every campaign it had open every three seconds, issuing both
`GET /api/campaigns/{id}/log` and `GET /api/campaigns/{id}/play` on each tick, plus
`GET /api/site-chat` on the All Campaigns page. Each of those requests loaded the full campaign
aggregate through `ICampaignStore.FindByIdAsync`, which runs a split query across six collections
and deserializes the overlay graph, catalog, and play-state JSON.

The result was roughly fifteen to twenty database statements per second for every open browser
tab. With only a handful of users, the Render instance reported sustained high CPU and memory.
Polling was the dominant cost, not asset bandwidth or payload size.

Lengthening the interval trades responsiveness for load without removing the waste: almost every
poll returned state the client already had.

## Decision

Push campaign change notifications to clients over Server-Sent Events instead of polling.

- `GET /api/campaigns/{campaignId}/stream` returns `text/event-stream` for an authorized viewer.
- `GET /api/site-chat/stream` does the same for the public site chat board.
- Events carry only a change kind and the campaign revision. They never carry campaign payloads.
- The client refetches the existing authorized endpoint only when the pushed revision is newer
  than what it has applied.
- `ICampaignUpdateBroadcaster` is an Application port. Infrastructure implements it as an
  in-process fan-out over bounded `System.Threading.Channels` channels keyed by campaign.
- Publication happens where the application already knows a campaign changed:
  `CampaignNotificationPublisher`, `PostCampaignChatHandler`, and the site-chat handler.
- A `PhaseDeadlineWorker` hosted service sleeps until the next stored phase deadline and runs the
  existing idempotent advance command, because the removed poll is what previously triggered
  time-based phase transitions.

### Why events carry no payload

Hidden orders, relics, and private objectives must not reach unauthorized clients. A broadcaster
that fanned out campaign state would have to re-apply per-viewer visibility filtering on a second
code path, which is a secrecy bug waiting to happen. Publishing only `{ kind, revision }` keeps
every visibility decision in the existing request handlers, which already separate public,
participant-private, and staff response shapes.

### Why in-process fan-out

ADR 0003 deploys a single API container. An in-memory broadcaster needs no broker, no new cloud
dependency, and no change to the modular monolith in ADR 0001.

**This is a single-instance design.** If the API is ever scaled to more than one instance, a
subscriber connected to instance A will not observe a mutation served by instance B. Moving to
multiple instances requires replacing the in-process fan-out with PostgreSQL `LISTEN`/`NOTIFY`
(no new infrastructure) or an external pub/sub broker (a further ADR). The broadcaster is behind
an Application port precisely so that swap does not touch endpoints or handlers.

## Consequences

- Steady-state request volume for an idle open campaign drops from roughly forty requests per
  minute per tab to zero, plus one named `heartbeat` event every twenty seconds. The beat is a
  real SSE event, not a comment, because `EventSource` does not expose comments to JavaScript.
- Each open tab holds a long-lived HTTP connection. These are asynchronous and hold no thread,
  but they do consume a Kestrel connection slot.
- The stream handler must not retain a scoped `CampaignDbContext`. The request scope lives for
  the whole connection, so a scoped context would pin a pooled Npgsql connection for as long as
  the client stays open and would exhaust the pool. Authorization runs in an explicit child scope
  that is disposed before the event loop begins, using the narrow
  `ICampaignStore.FindForAccessCheckAsync` snapshot rather than a full campaign load.
- Clients keep a sixty-second safety-net poll that runs **only while the stream is disconnected**,
  so a proxy that refuses `text/event-stream` degrades to slower updates rather than breaking.
  An open stream that delivers no heartbeat or update for forty-five seconds is treated as
  disconnected: a buffering Worker still looks `open` to `EventSource`, and that stall is the
  only automatic way to notice.
- `EventSource` cannot send custom headers and depends on cookies. The Angular app ships
  `apiBaseUrl: ""` and calls same-origin `/api`, and the auth cookie is `SameSite=Lax`, so this
  works with the current topology. Moving the API to a different origin would require CORS with
  credentials and `SameSite=None`.
- Responses set `X-Accel-Buffering: no` and are excluded from response buffering so intermediate
  proxies forward events promptly.
- `GET /play` keeps persisting an automatic phase advance as a documented safety net, as described
  in `docs/ARCHITECTURE.md`. It simply runs far less often.

## Alternatives considered

- **WebSockets.** Bidirectional framing is not needed; all client intent already travels as
  authenticated HTTP commands. SSE reuses cookie auth, reconnects natively, and needs no new
  client library.
- **Longer poll intervals.** Cheaper to implement but keeps the wasteful full-aggregate read and
  makes chat noticeably slower.
- **Cheap revision-probe endpoint.** Would reduce cost substantially while keeping polling, but
  still scales request volume with users and tabs.
