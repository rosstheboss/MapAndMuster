# Database backup and restore

Managed PostgreSQL backups are owned by Render and by you. This repository only documents
how to verify them and how to take an independent logical dump.

Never put passwords in Git, scripts, or tickets. Use a password manager and environment
variables.

## What to verify in Render

Open the production database (`mapandmuster-db`) in [dashboard.render.com](https://dashboard.render.com):

- [ ] Automatic backups / point-in-time recovery are enabled for the plan you bought
- [ ] Retention covers at least one full campaign planning cycle (longer is better)
- [ ] You know how to restore from the dashboard onto a **new** instance (Render's docs
      for your plan)
- [ ] The restore target is never the live production instance until you have tested on a
      copy

Render's own restore is the fast path after a platform failure. Do not treat it as the
only copy.

## Independent logical dump

Keep a `pg_dump` somewhere that is **not** Render (encrypted disk, object storage you
control, or an offline drive). Do this before a real campaign starts, and after any
migration you care about.

Use the **External** `postgres://` URI from Render → database → **Connections**. The
ASP.NET `Host=...;Username=...` form is for the API, not for `pg_dump`.

```powershell
./scripts/pg-dump.ps1 -ConnectionString "<RENDER_DATABASE_URL>" -OutputPath ".\backups\campaign.dump"
```

```bash
ConnectionStrings__Campaign="<RENDER_DATABASE_URL>" BACKUP_PATH=./backups/campaign.dump ./scripts/pg-dump.sh
```

The scripts refuse an empty connection string and require a `postgres://` or
`postgresql://` URI. They run `pg_dump` inside `postgres:17` so you do not need a local
PostgreSQL install. Docker must be able to reach Render (allow your IP on the database if
you restricted `ipAllowList`).

Custom format (`--format=custom`) is the default so restore can be selective later.

## Restore onto a non-production database

Restore is destructive for the **target**. The scripts require an explicit confirmation
phrase and still cannot know whether the URI is production. You must choose a staging or
throwaway database.

```powershell
./scripts/pg-restore.ps1 `
  -ConnectionString "<STAGING_RENDER_DATABASE_URL>" `
  -BackupPath ".\backups\campaign.dump" `
  -Confirm "I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE"
```

```bash
CONFIRM_RESTORE=I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE \
ConnectionStrings__Campaign="<STAGING_RENDER_DATABASE_URL>" \
BACKUP_PATH=./backups/campaign.dump \
./scripts/pg-restore.sh
```

After restore:

1. Point a staging API at that database (or run smoke tests against staging).
2. Sign in, open a campaign list, and confirm row counts you expect.
3. Only then consider the backup valid.

Restore uses `--no-owner --no-acl` so Render's `mapandmuster_user` grants are not applied
on a local or staging database that does not have that role. Tables and data still restore.

Never run `pg-restore` against production to "test" a dump.

## How to decide the backup is good

A backup is not proven until a restore has booted an application against it. Dashboard
"backup succeeded" is necessary but not sufficient. Before the first real campaign:

1. Dump production (or the soon-to-be-production empty-migrated database).
2. Restore onto staging or a disposable Render Postgres.
3. Run `./scripts/smoke-test.ps1` against that environment.
4. Spot-check data that would hurt to lose (users, campaigns, orders).

Repeat after the first production migration.

## Migrations and backups

Take a dump **before** applying `eng/run-migrations.*` to production. If a migration goes
badly, restore onto a copy, inspect, and decide. Do not roll back a destructive migration
in place. Expand/contract guidance is in `docs/deployment.md`.
