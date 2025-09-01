#!/usr/bin/env sh
set -euo pipefail

# Allow overriding config path via AGENT_CONFIG_PATH; default already set in Dockerfile
exec /app/Agent.Template
