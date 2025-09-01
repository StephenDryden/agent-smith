#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/../docker/docker-compose.yml"
export SLITE_API_KEY="${SLITE_API_KEY:-}"
export GITHUB_MODELS_TOKEN="${GITHUB_MODELS_TOKEN:-}"

docker compose -f "$COMPOSE_FILE" up --build -d

echo "Agents running: slite on :8081, newrelic on :8082"
