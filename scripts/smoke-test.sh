#!/usr/bin/env bash
set -euo pipefail

frontend_url="${FRONTEND_URL:-}"
api_url="${API_URL:-}"

usage() {
  echo "Usage: FRONTEND_URL=https://<ROOT_DOMAIN> [API_URL=https://<ROOT_DOMAIN>] $0" >&2
  echo "API_URL defaults to FRONTEND_URL when omitted (same-origin /api and /health)." >&2
  exit 1
}

if [ -z "${frontend_url}" ]; then
  usage
fi

if [ -z "${api_url}" ]; then
  api_url="${frontend_url}"
fi

frontend_url="${frontend_url%/}"
api_url="${api_url%/}"

check_status() {
  local name="$1"
  local url="$2"
  local expected_snippet="${3:-}"
  local body
  local http_code

  body="$(mktemp)"
  http_code="$(curl -fsS -o "${body}" -w '%{http_code}' --max-time 30 "${url}")" || {
    echo "FAIL ${name}: request failed for ${url}" >&2
    rm -f "${body}"
    exit 1
  }

  if [ "${http_code}" != "200" ]; then
    echo "FAIL ${name}: expected HTTP 200 from ${url}, got ${http_code}" >&2
    rm -f "${body}"
    exit 1
  fi

  if [ -n "${expected_snippet}" ] && ! grep -q "${expected_snippet}" "${body}"; then
    echo "FAIL ${name}: response from ${url} did not contain expected content" >&2
    rm -f "${body}"
    exit 1
  fi

  rm -f "${body}"
  echo "OK   ${name}"
}

check_status "frontend" "${frontend_url}/" "app-root"
check_status "api live" "${api_url}/health/live" '"status":"Healthy"'
check_status "api ready" "${api_url}/health" '"status":"Healthy"'

echo "Smoke tests passed."
