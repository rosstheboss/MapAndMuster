#!/usr/bin/env bash
set -euo pipefail

connection="${1:-${ConnectionStrings__Campaign:-}}"
if [ -z "${connection}" ]; then
  echo "Set ConnectionStrings__Campaign or pass the connection string as the first argument." >&2
  echo "Refusing to migrate an unspecified database." >&2
  exit 1
fi

root="$(cd "$(dirname "$0")/.." && pwd)"
bundle="${root}/artifacts/efbundle"
if [ ! -x "${bundle}" ] && [ -x "${bundle}.exe" ]; then
  bundle="${bundle}.exe"
fi

if [ ! -x "${bundle}" ]; then
  echo "No migration bundle found. Run eng/build-migrations.sh first." >&2
  exit 1
fi

"${bundle}" --connection "${connection}"
