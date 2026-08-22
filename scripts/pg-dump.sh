#!/usr/bin/env bash
set -euo pipefail

connection="${1:-${ConnectionStrings__Campaign:-}}"
output="${2:-${BACKUP_PATH:-}}"

if [ -z "${connection}" ]; then
  echo "Set ConnectionStrings__Campaign or pass the connection string as the first argument." >&2
  echo "Refusing to dump an unspecified database." >&2
  exit 1
fi

case "${connection}" in
  postgres://*|postgresql://*) ;;
  *)
    echo "Pass a postgres:// URI from the Render dashboard, not an ASP.NET Host= connection string." >&2
    exit 1
    ;;
esac

if [ -z "${output}" ]; then
  output="campaign-$(date -u +%Y%m%dT%H%M%SZ).dump"
fi

output_dir="$(cd "$(dirname "${output}")" && pwd)"
output_file="$(basename "${output}")"

echo "Writing a custom-format dump to ${output_dir}/${output_file}"
docker run --rm \
  -e PGDATABASE_URI="${connection}" \
  -e OUTPUT_FILE="${output_file}" \
  -v "${output_dir}:/backup" \
  postgres:17 \
  sh -c 'pg_dump --dbname="$PGDATABASE_URI" --format=custom --no-owner --file="/backup/$OUTPUT_FILE"'

echo "Dump finished. Store this file outside the primary database provider."
