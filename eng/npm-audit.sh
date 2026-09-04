#!/usr/bin/env bash
# Retries npm audit when the registry advisory API times out.
# High and critical findings still fail immediately.
set -euo pipefail

attempts="${NPM_AUDIT_ATTEMPTS:-5}"
delay="${NPM_AUDIT_RETRY_SECONDS:-15}"
level="${NPM_AUDIT_LEVEL:-high}"

is_transient() {
  grep -Eqi \
    'audit endpoint returned an error|network timeout|ECONNRESET|ETIMEDOUT|ENOTFOUND|EAI_AGAIN|socket hang up|503 Service Unavailable|502 Bad Gateway|429 Too Many Requests' \
    <<<"$1"
}

attempt=1
while (( attempt <= attempts )); do
  set +e
  output="$(npm audit --audit-level="$level" 2>&1)"
  status=$?
  set -e
  printf '%s\n' "$output"

  if (( status == 0 )); then
    exit 0
  fi

  if is_transient "$output" && (( attempt < attempts )); then
    echo "npm audit endpoint failed (attempt ${attempt}/${attempts}); retrying in ${delay}s..." >&2
    sleep "$delay"
    delay=$(( delay * 2 ))
    attempt=$(( attempt + 1 ))
    continue
  fi

  if is_transient "$output"; then
    echo "npm audit could not reach the advisory endpoint after ${attempts} attempts." >&2
  fi

  exit "$status"
done
