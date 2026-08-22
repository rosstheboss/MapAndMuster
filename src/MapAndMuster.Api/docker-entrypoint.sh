#!/bin/sh
set -e

if [ -n "${PORT}" ]; then
  export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"
  unset ASPNETCORE_HTTP_PORTS
fi

exec dotnet MapAndMuster.Api.dll
