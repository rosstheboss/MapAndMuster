#!/usr/bin/env bash
set -euo pipefail

connection="${1:-${ConnectionStrings__Campaign:-}}"
backup="${2:-${BACKUP_PATH:-}}"
confirm="${CONFIRM_RESTORE:-}"

if [ -z "${connection}" ] || [ -z "${backup}" ]; then
  echo "Usage: CONFIRM_RESTORE=I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE \\" >&2
  echo "       ConnectionStrings__Campaign=<postgres:// URI> BACKUP_PATH=<file.dump> $0" >&2
  echo "Restore only onto a non-production database." >&2
  exit 1
fi

if [ "${confirm}" != "I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE" ]; then
  echo "Set CONFIRM_RESTORE=I_UNDERSTAND_THIS_OVERWRITES_THE_TARGET_DATABASE to continue." >&2
  echo "This command overwrites the target database. Never point it at production." >&2
  exit 1
fi

case "${connection}" in
  postgres://*|postgresql://*) ;;
  *)
    echo "Pass a postgres:// URI from the Render dashboard, not an ASP.NET Host= connection string." >&2
    exit 1
    ;;
esac

if [ ! -f "${backup}" ]; then
  echo "Backup file not found." >&2
  exit 1
fi

backup_dir="$(cd "$(dirname "${backup}")" && pwd)"
backup_file="$(basename "${backup}")"

echo "Restoring ${backup_dir}/${backup_file} onto the target database"
docker run --rm \
  -e PGDATABASE_URI="${connection}" \
  -e BACKUP_FILE="${backup_file}" \
  -v "${backup_dir}:/backup" \
  postgres:17 \
  sh -c 'pg_restore --clean --if-exists --no-owner --no-acl --dbname="$PGDATABASE_URI" "/backup/$BACKUP_FILE"'

echo "Restore finished. Validate the target application before considering the backup good."
