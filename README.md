[![CI](https://github.com/StephenDryden/agent-smith/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/StephenDryden/agent-smith/actions/workflows/ci.yml)

# agent-smith
AI agent template (.NET 8) that exposes an OpenAI-style chat API and connects to Model Context Protocol (MCP) servers over stdio or streamable HTTP (SSE). It’s config-driven (one JSON per agent), containerized (one image per config), and includes diagnostics to list/call MCP tools for easy integration testing.

## Features
- OpenAI-style POST /v1/chat (non-streaming) with a configurable persona/system prompt
- Config-driven agents (JSON under `configs/`) with `${VAR}` environment substitution
- MCP connectivity via:
	- stdio (e.g., `npx slite-mcp`) with cancellable reads to avoid hangs
	- streamable HTTP with SSE parsing and JSON-RPC handling
- Provider abstraction with:
	- GitHub Models (real) using `GITHUB_MODELS_TOKEN`
	- Mock provider for local/offline development
- Diagnostics endpoints to validate MCP wiring without the model:
	- `GET /mcp/tools` and `POST /mcp/call`
- Container-first: one image per config via build args; Docker Compose services per agent
- Health checks and pragmatic timeouts for stable local and container runs
- CI workflow for build/test and image builds (ECR push scaffolded)

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
 - GET /mcp/tools -> list MCP tools (diagnostic)
 - POST /mcp/call -> call an MCP tool by name (diagnostic)

Request body:
{ "messages": [{ "role": "user", "content": "Hello" }] }

Response body:
{ "message": { "role": "assistant", "content": "..." } }

## Configuration
- Persona/system prompt and provider parameters in JSON (see `configs/`)
- MCP:
	- stdio: uses Node/npm; for Slite the npm package is `slite-mcp-server` (CLI name: `slite-mcp`)
	- streamable-http: supports SSE response parsing

## Notes
- Provider: GitHub Models (requires GITHUB_MODELS_TOKEN)
- One image per config via compose build args
- Node/npm installed in container to support stdio MCP servers

See also:
- docs/slite-onboarding.md for Slite MCP setup/testing

## Add a new agent

You can add a new agent either manually or by asking GitHub Copilot to do it for you.

Manual steps:
1) Create a config: copy any file in `configs/` (e.g., `slite.agent.json`) to `configs/<your>.agent.json` and edit:
	- `agent`: name and `systemPrompt`
	- `model`: provider and `modelId` (env override via `MODEL_PROVIDER` is supported)
	- `mcp` (choose one):
	  - stdio: set `command` (e.g., `npx`) and `args` (e.g., `["-y","<your-mcp-server>"]`), plus any `env` like `${YOUR_API_KEY}`
	  - streamable-http: set `http.url`, and optionally `allowSse` and `timeoutMs`
	- `runtime.port`: internal port (container listens on 8080; host port is set in compose)
2) Wire it in Docker Compose: add a service to `docker/docker-compose.yml` using the same Dockerfile with `build.args.CONFIG_FILE: configs/<your>.agent.json`, set a unique host port mapping (e.g., `8083:8080`), include `env_file: ../.env`, and set `AGENT_CONFIG_PATH=/app/config/agent.json` in `environment`.
3) Update `.env`: add any required secrets referenced by your config (e.g., `YOUR_API_KEY`), plus `GITHUB_MODELS_TOKEN` for real model calls.
4) (Optional) Warm up npm-based stdio MCP servers once to avoid first-run delays:
	- `npx -y <your-mcp-server> --help`
5) Rebuild and run:
	- `docker compose -f docker/docker-compose.yml up -d --build`
6) Test:
	- Health: `curl http://localhost:<your-host-port>/healthz`
	- Chat: `POST /v1/chat` with a messages array
	- MCP diagnostics (if applicable): `GET /mcp/tools` and `POST /mcp/call`

Ask GitHub Copilot to add it for you:
- Tell GitHub Copilot: the MCP type (stdio vs streamable-http), server command/URL, any required env vars, the desired host port, and provider/model settings. It will create the config, update compose, and validate with health and chat calls.

More details:
- See `.github/instructions/repo.instruction.md` for architecture and MCP server guidance

## Testing

Docker Compose (recommended):

1) Ensure `.env` is configured (tokens/secrets and any MCP vars)
2) Start services
```bash
docker compose -f docker/docker-compose.yml up -d --build
```
3) Health checks
```bash
curl -sS http://localhost:8081/healthz | jq .    # slite
curl -sS http://localhost:8082/healthz | jq .    # newrelic
```
4) Chat test (slite)
```bash
curl -m 30 -sS -X POST http://localhost:8081/v1/chat \
	-H 'Content-Type: application/json' \
	-d '{"messages":[{"role":"user","content":"Say hi"}]}' | jq .
```
5) MCP diagnostics (slite)
```bash
curl -sS http://localhost:8081/mcp/tools | jq .
curl -sS -X POST http://localhost:8081/mcp/call \
	-H 'Content-Type: application/json' \
	-d '{"name":"ask-slite","arguments":{"question":"What are my last 3 notes?"}}' | jq .
```

Mock vs real provider:
- Fast local: set `MODEL_PROVIDER=mock` and optionally `SKIP_MCP_INIT=true` in `.env`, rebuild, then call `/v1/chat`.
- Real model: set `MODEL_PROVIDER=github-models`, ensure `GITHUB_MODELS_TOKEN` is set, and keep `SKIP_MCP_INIT=false` for MCP.

Slite stdio warm-up (avoids first-run delays):
```bash
set -a; source .env; set +a
SLITE_API_KEY="$SLITE_API_KEY" npx -y slite-mcp --help >/dev/null 2>&1 || true
```

Local (without Docker):
```bash
ASPNETCORE_URLS=http://localhost:8080 \
	AGENT_CONFIG_PATH=$PWD/configs/slite.agent.json \
	dotnet run --project src/Agent.Template/Agent.Template.csproj

# In another terminal
curl -sS http://localhost:8080/healthz | jq .
curl -sS -X POST http://localhost:8080/v1/chat \
	-H 'Content-Type: application/json' \
	-d '{"messages":[{"role":"user","content":"Say hi"}]}' | jq .
```

CI
- Pull requests trigger build/test and Docker image builds (see `.github/workflows/ci.yml`).

### Querying the Agent Commander

The Agent Commander aggregates responses from multiple agents and provides real-time feedback. It includes endpoints for health checks and listing discovered agents.

#### Using Docker Compose
1. Start the services:
   ```bash
   docker-compose up
   ```
2. Query the Agent Commander:
   - **Health Check**:
     ```bash
     curl -X GET http://localhost:8083/health
     ```
   - **List Agents**:
     ```bash
     curl -X GET http://localhost:8083/agents
     ```

#### Interacting with the Slite Agent via the Agent Commander
1. Ensure the Slite agent is running (http://localhost:8081).
2. Send a message to the Agent Commander to query the Slite agent:
   ```bash
   curl -sS -X POST http://localhost:8083/mcp/call -H 'Content-Type: application/json' -d '{"name":"ask-slite","arguments":{"question":"show me information on the team structure?"}}' | jq .
   ```
