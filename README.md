# agent-smith
AI agent template (.NET 8) with MCP connectivity (Streamable HTTP + stdio) and an OpenAI-style chat API.

## Quickstart

Prereqs:
- .NET 8 SDK
- Docker (for containerized run) and Docker Compose
- GitHub Models token with models access

Setup:
1) Copy env file and fill values
	 - cp .env.example .env
	 - Set GITHUB_MODELS_TOKEN and, if using Slite MCP, SLITE_API_KEY
2) Pick a config in `configs/` (e.g., `slite.agent.json` stdio, `newrelic.agent.json` streamable-http)

### Run with Docker Compose
- scripts/run-local.sh
- Services:
	- slite agent: http://localhost:8081
	- newrelic agent: http://localhost:8082

Test:
- scripts/curl/chat.sh http://localhost:8081

### Run locally (without Docker)
Option A: default (no config) — minimal run; requires GITHUB_MODELS_TOKEN set in env
- ASPNETCORE_URLS=http://localhost:8080 dotnet run --project src/Agent.Template/Agent.Template.csproj
- scripts/curl/chat.sh http://localhost:8080

Option B: with config
- Set AGENT_CONFIG_PATH to an absolute path of a config JSON
- Example: AGENT_CONFIG_PATH=$PWD/configs/slite.agent.json ASPNETCORE_URLS=http://localhost:8080 dotnet run --project src/Agent.Template/Agent.Template.csproj

## Endpoints
- GET / -> service heartbeat
- GET /healthz -> health
- POST /v1/chat -> OpenAI-style chat (non-streaming)

Request body:
{ "messages": [{ "role": "user", "content": "Hello" }] }

Response body:
{ "message": { "role": "assistant", "content": "..." } }

## Configuration
- Persona/system prompt and provider parameters in JSON (see `configs/`)
- MCP:
	- stdio: uses Node/npm; for Slite the package is `slite-mcp-server`
	- streamable-http: supports SSE response parsing

## Notes
- Provider: GitHub Models (requires GITHUB_MODELS_TOKEN)
- One image per config via compose build args
- Node/npm installed in container to support stdio MCP servers

See also:
- docs/slite-onboarding.md for Slite MCP setup/testing
