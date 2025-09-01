#!/usr/bin/env bash
set -euo pipefail
HOST="${1:-http://localhost:8081}"

curl -sS -X POST "$HOST/v1/chat" \
  -H 'Content-Type: application/json' \
  -d '{
    "messages": [
      {"role":"user","content":"Hello agent, what is your persona?"}
    ]
  }' | jq .
