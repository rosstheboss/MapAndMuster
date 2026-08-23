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

normalize_connection() {
  python3 - "$1" <<'PY'
import sys
from urllib.parse import urlparse

value = sys.argv[1].strip().strip('"').strip("'").lstrip("<").rstrip(">")
if value.startswith("\ufeff"):
    value = value.lstrip("\ufeff")
if not value.lower().startswith(("postgres://", "postgresql://")):
    print(value, end="")
    raise SystemExit(0)

parsed = urlparse(value)
if not parsed.hostname:
    sys.stderr.write(
        "Could not parse the Render URL. Copy only the External Database URL.\n"
    )
    raise SystemExit(1)


def quote(text: str) -> str:
    return "'" + text.replace("'", "''") + "'"


user = parsed.username or ""
password = parsed.password or ""
database = (parsed.path or "").lstrip("/")
port = parsed.port or 5432
print(
    ";".join(
        [
            f"Host={parsed.hostname}",
            f"Port={port}",
            f"Database={quote(database)}",
            f"Username={quote(user)}",
            f"Password={quote(password)}",
            "SSL Mode=Require",
            "Trust Server Certificate=true",
        ]
    ),
    end="",
)
PY
}

normalized="$(normalize_connection "${connection}")"
"${bundle}" --connection "${normalized}"
