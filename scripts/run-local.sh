#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/../docker/docker-compose.yml"
export SLITE_API_KEY="${SLITE_API_KEY:-}"
export GITHUB_MODELS_TOKEN="${GITHUB_MODELS_TOKEN:-}"

docker compose -f "$COMPOSE_FILE" up --build -d

echo "Agents running: slite on :8081, newrelic on :8082, commander on :8083"

# Function to check health and print /tools result
check_agent() {
  local name="$1"
  local port="$2"

  echo "Checking health of $name agent on port $port..."
  local health_response
  health_response=$(curl -m 5 -sS "http://localhost:$port/healthz" | jq -r '.status')

  if [[ "$health_response" == "healthy" ]]; then
    echo "$name agent is healthy."
    echo "Fetching /tools for $name agent..."
    curl -m 5 -sS "http://localhost:$port/mcp/tools" | jq .
  else
    echo "$name agent is not healthy. Response: $health_response"
  fi
}

# Fetch and display tools available via the Commander Agent
fetch_commander_tools() {
  local port=8083

  echo "Fetching tools available via the Commander Agent..."
  local tools_response
  tools_response=$(curl -m 5 -sS "http://localhost:$port/tools")

  if [[ -n "$tools_response" ]]; then
    echo "Tools available via connected agents:"
    echo "$tools_response" | jq -r 'to_entries[] | "Agent: \(.key), Tools: \(.value | join(", "))"'
  else
    echo "Failed to fetch tools from the Commander Agent."
  fi
}

# Check health and tools for each agent
check_agent "slite" 8081
#check_agent "newrelic" 8082
fetch_commander_tools
