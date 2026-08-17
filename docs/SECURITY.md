# Security, Privacy, and Audit Requirements

## Data classification

- **Public:** revealed map state, standings, public objectives, configured public faction rules,
  the campaign log, in-campaign chat, and the live play board returned to viewers (force
  positions and revealed facts; drafts and unrevealed orders remain omitted).
- **Participant-private:** own drafts/orders, private objectives, account data, eligible choices.
- **Shared-private:** faction/alliance objective data visible only to authorized group members.
- **Staff-sensitive:** unrevealed orders/relics, correction tools, moderation notes, full audit.
- **Secret:** credentials, tokens, signing keys, database/email/storage secrets.

Authorization is enforced when querying and mapping data, not only in the Angular UI. Prefer
separate response models for public, participant, and staff views.

## Authentication

- ASP.NET Core Identity with verified email, secure password reset, lockout, and rate limiting.
- Local passwords must be at least 12 characters and include uppercase, lowercase, a number,
  and a special character. Changing a password while signed in requires the current password.
- Usernames and legal names reject English profanity, racial slurs, and similar abusive terms.
- Usernames that collide with chat recipients or system keywords (everyone, public, private,
  here, and similar words) are reserved.
- Registration and profile updates require username, first name, last name, city, state or
  province, country, and time zone. Middle initial, suffix, and avatar are optional.
- Secure, HTTP-only, same-site cookies for the same-origin web application.
- Secure, HTTP-only, same-site cookies for the same-origin web application.
- External providers are optional and configuration-gated. A matching email does not auto-link;
  the player must sign in to the existing verified account.
- Other users receive only username, location, avatar, the chosen display name, and campaigns
  they may already view (publicly viewable campaigns plus private campaigns they share). Email,
  created/updated timestamps, time-zone preference, the legal name (unless the owner opted
  to show full name), join passwords, and hidden private campaigns are omitted from public
  profile responses.
- Never log passwords, tokens, reset links, cookies, private objectives, or hidden locations.

## Authorization

- System Administrator is global; Player and Game Master memberships are campaign-scoped.
- Use named permission policies. Do not assume a GM is neutral or is not also a player.
- Every command revalidates membership, effective actor, entity ownership, campaign state,
  revision, and deadline.
- Staff inspection of unrevealed data is purposeful, audited, and notified. Campaign debug mode
  is that inspection path: entering, each correction, and exiting are logged. Unrevealed order
  kinds are omitted from public debug-correction summaries while an action window is still open.

## Auditing

Audit records are append-only application facts containing:

- event type and campaign;
- actual and effective actors;
- affected entity and non-secret summary;
- UTC timestamp;
- reason for staff intervention;
- prior and resulting campaign revisions;
- before/after values where safe;
- notification/outbox references.

Do not place secret values in broadly visible audit summaries. Maintain protected details where
staff review requires them.

## Uploads and content

- Allow-list image/document types and validate content, size, and decoded dimensions.
- Store uploads outside the web root with generated storage names.
- Serve downloads through authorized endpoints or scoped object-storage URLs.
- Strip metadata where appropriate and re-encode raster images when feasible.
- Reject or sanitize active HTML/SVG. Do not support arbitrary scripts or administrator code.
- Generic seed content must not contain proprietary game text or artwork.
- Private-campaign join passwords are hashed. They are never returned in API payloads.
- Join privacy (`IsPrivate`) is separate from public viewing (`IsPubliclyViewable`). Hidden
  campaigns return 404 to non-members for detail, map, and catalog reads after the caller is
  authorized as a signed-in user. Upcoming hidden campaigns remain listable on All Campaigns so
  players can join.

## Notifications

In-app and email notifications are created through a transactional outbox. Email content must
avoid exposing hidden order/relic/objective details and must never include private chat bodies;
direct the recipient to authenticate for sensitive content. Private campaign chat is omitted from
unauthorized API payloads, including campaign-manager views. Only a system administrator who is
the active debug actor on that campaign may inspect other members' private chats.

## Operational baseline

- Secrets come from development secret storage or deployment configuration, never Git.
- Apply database migrations deliberately and back up production campaign data.
- Enable structured security/audit logging without sensitive payloads.
- Rate-limit registration, login, password reset, uploads, and high-impact staff actions.
- Maintain dependency and static-analysis checks in CI.
