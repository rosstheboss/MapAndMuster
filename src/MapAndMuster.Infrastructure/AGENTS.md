# Infrastructure Project Instructions

These instructions extend the repository `AGENTS.md`.

- Implement Application ports without leaking provider types across the boundary.
- PostgreSQL is production-equivalent; migrations and integration tests target PostgreSQL.
- Enforce database constraints and optimistic concurrency in addition to domain checks.
- Store UTC times, append audit/history, and never overwrite original submissions.
- Use a transactional outbox for email and other external side effects.
- Keep secrets out of source, fixtures, logs, and exception messages.
- Upload handlers validate, sanitize/re-encode where appropriate, and store outside web root.
