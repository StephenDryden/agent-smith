#!/usr/bin/env sh
set -e
wget -qO- http://localhost:8080/healthz >/dev/null 2>&1 || exit 1
