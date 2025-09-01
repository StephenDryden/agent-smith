---
applyTo: '**'
---

# Agent Smith – Repository Guide for Assistants and Contributors

This repo provides a config-driven .NET 8 agent service that exposes an OpenAI-style POST /v1/chat endpoint, connects to Model Context Protocol (MCP) servers (via stdio or streamable HTTP), and runs containerized per-agent configurations.

## Architecture at a glance
- API: ASP.NET Core Minimal API (non-streaming chat endpoint)
- Config: JSON per agent under `configs/`, with env var substitution `${VAR}`
- MCP Transports:
  - stdio: launches a process (e.g., `npx slite-mcp-server`) and exchanges newline-delimited JSON
  - streamable-http: connects to an HTTP/SSE MCP server
- Providers:
  - GitHub Models (real)
  - Mock (local/offline)
- Docker/Compose: one image per config; ports mapped for local testing

Key folders:
- `src/Agent.Template` – Web API host and request pipeline
- `src/Agent.Core` – Core types (config, chat DTOs)
- `src/Agent.Mcp` – MCP client and transports
- `src/Agent.Providers` – Model providers (GitHub Models, Mock)
- `configs/` – Per-agent JSON configs (e.g., `slite.agent.json`)
- `docker/` – Dockerfile and docker-compose
- `docs/` – Additional onboarding docs (e.g., Slite)

Important environment variables:
- `AGENT_CONFIG_PATH` – Path to baked/loaded agent config JSON (default `/app/config/agent.json`)
- `MODEL_PROVIDER` – Overrides provider from config (e.g., `mock`, `github-models`)
- `GITHUB_MODELS_TOKEN` – Token for GitHub Models API
- `SLITE_API_KEY` – API key for Slite MCP server
- `SKIP_MCP_INIT` – If `true`, skips MCP handshake on requests

## Request/Response contract
- Endpoint: `POST /v1/chat`
- Request: `{ messages: [{ role: "system"|"user"|"assistant", content: string }] }`
- Response: `{ message: { role: "assistant", content: string } }`
- Behavior:
  - Prepends `agent.systemPrompt` from config as an initial system message
  - Best-effort MCP initialize unless `SKIP_MCP_INIT=true`
  - Calls provider for completion (GitHub Models by default; Mock when configured)

## Run it locally
1) Copy `.env.example` to `.env` and fill secrets
2) For Docker: `docker compose -f docker/docker-compose.yml up -d --build`
3) Health check: `GET /healthz` on mapped port
4) Chat: POST to `/v1/chat` with a messages array

Tip: For stdio MCP servers installed via npm, warm up once with `npx <server> --help` to avoid first-run delays.

## Add a new MCP server

Option A: stdio MCP server
1) Create a new config under `configs/your.agent.json`:
   - `mcp.transport = "stdio"`
   - `mcp.stdio.command` (e.g., `npx` or binary)
   - `mcp.stdio.args` (e.g., `["-y", "your-mcp-server"]`)
   - `mcp.stdio.env` mapping for any required secrets, using `${VAR}` placeholders
   - Set `runtime.port`
2) Add a new service to `docker/docker-compose.yml`:
   - Use the same Dockerfile
   - Provide `build.args.CONFIG_FILE: configs/your.agent.json`
   - Map a unique host port to container `8080`
   - Include `env_file: ../.env` so secrets are available
3) Ensure secrets exist in `.env`
4) Warm up (optional for npm-based servers): `npx -y your-mcp-server --help`
5) Rebuild and run compose; hit `/healthz` and `/v1/chat`

Option B: streamable-http MCP server
1) Create `configs/your.agent.json` with:
   - `mcp.transport = "streamable-http"`
   - `mcp.http.url` set to the MCP endpoint
   - `mcp.http.allowSse` and `mcp.http.timeoutMs` as needed
   - `runtime.port` and other settings
2) Add a compose service pointing to that config
3) Rebuild and test

Notes & tips:
- The app performs a best-effort `initialize` JSON-RPC call and drains a few messages; set `SKIP_MCP_INIT=true` to bypass
- The stdio transport respects cancellation to avoid hanging; ensure your server emits newline-delimited JSON
- Troubleshoot with `docker logs <container>` and increase client timeouts during first run

## Add a new model provider
1) Implement `IModelProvider` in `src/Agent.Providers`
2) Add env var(s) for credentials; document them in `.env.example`
3) Reference provider by name via config (`model.provider`) and optional env override (`MODEL_PROVIDER`)
4) Add minimal tests or a mock for local development

## Troubleshooting
- Timeouts on first run: warm up npm-based MCP servers; increase request timeout; retry
- Unauthorized from provider: verify tokens in `.env` and container env
- Port conflicts: adjust compose port mappings
- Missing config: confirm `AGENT_CONFIG_PATH` and baked file path

## Diagnostics
- Service exposes helper endpoints to validate MCP plumbing without the model:
  - `GET /mcp/tools` – returns the MCP tools list (via `tools/list`)
  - `POST /mcp/call` – body: `{ name: string, arguments?: object }` (invokes `tools/call`)

## Documentation maintenance (keep docs in sync)
Whenever you make relevant code changes, update this instructions file and affected READMEs in the same PR. Examples that require doc updates:
- New or changed HTTP endpoints (e.g., added `/mcp/tools`, `/mcp/call`, or changed `/v1/chat` contract)
- Configuration shape or defaults (e.g., new keys under `model`, `mcp`, `runtime`, `security`)
- Environment variables (add/remove/change semantics)
- Provider behavior (new provider, parameters, auth)
- MCP transport behavior (timeouts, protocol expectations, command/args)

PR checklist (docs):
- [ ] Updated `.github/instructions/repo.instruction.md` if architecture, flows, or setup changed
- [ ] Updated root `README.md` (Quickstart, Endpoints, Notes, links)
- [ ] Updated project-level READMEs if public behavior or responsibilities changed
- [ ] Updated `configs/README.md` if adding new agent configs or patterns
