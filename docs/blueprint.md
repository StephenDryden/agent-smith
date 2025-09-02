# Agent Smith Template Blueprint

This document captures the final decisions and a repeatable set of instructions to scaffold, build, and run a C# agent template that interacts with MCP servers via Streamable HTTP (with SSE) and stdio, with per-config container images. Use this as the single source of truth to reproduce the setup without re-asking questions.

## Final decisions (defaults)

- Language and runtime
  - .NET: 8 (SDK pinned via `global.json`).
  - OS/arch: linux/arm64.
  - Container base: Alpine (aspnet:8.0-alpine for runtime; sdk:8.0-alpine for build).

- Agent HTTP API (public)
  - Non-streaming in v1.
  - Endpoints: `POST /v1/chat` (OpenAI-style messages), `GET /healthz`.
  - No authentication for now; placeholders wired for future bearer auth.

- MCP transports (to external MCP servers)
  - Streamable HTTP: compliant with spec (Accept includes `application/json, text/event-stream`), supports SSE for POST responses. Optional GET “listening” is deferred.
  - stdio: newline-delimited JSON-RPC; child process lifecycle handled by the agent.
  - Transport is selected via config per agent.

- Slite MCP support
  - Use stdio via Node-based `slite-mcp` CLI (npm package: `slite-mcp-server`, launched with `npx`).
  - Node/npm included in the agent image for Slite builds.

- Provider for LLM
  - GitHub Models API (OpenAI-style chat).
  - Default model: `openai/gpt-4o-mini`.
  - Token supplied via env: `GITHUB_MODELS_TOKEN` (PAT with models scope).

- Configuration
  - JSON format, baked into the image; one image per config.
  - Secrets are not baked; provided via environment variables at runtime.
  - Config defines the agent persona via `systemPrompt`.

- Local development
  - docker-compose to run multiple agent images.
  - Host ports: 8081 (slite), 8082 (newrelic placeholder) mapped to container 8080.

- Observability
  - Console logging only. Metrics and tracing deferred.

References
- MCP Streamable HTTP spec: https://modelcontextprotocol.io/specification/2025-03-26/basic/transports#streamable-http
- GitHub Models quickstart: https://docs.github.com/en/github-models/quickstart

---

## Repository structure (target)

```
agent-smith/
├─ README.md
├─ LICENSE
├─ .editorconfig
├─ .gitignore
├─ global.json
├─ Directory.Build.props
├─ Directory.Packages.props
│
├─ docs/
│  ├─ blueprint.md                  # This file
│  ├─ architecture.md               # High-level design and data flow
│  ├─ api.md                        # Agent HTTP API reference
│  ├─ config-schema.md              # JSON schema documentation
│  ├─ mcp-transport.md              # Streamable HTTP + SSE + stdio behavior
│  └─ local-dev.md                  # docker-compose usage, curl examples
│
├─ configs/
│  ├─ slite.agent.json              # stdio to Slite MCP (CLI `slite-mcp`), persona + model
│  ├─ newrelic.agent.json           # placeholder HTTP transport
│  ├─ commander.agent.json           # Agent Commander config
│  └─ samples/minimal.agent.json
│
├─ src/
│  ├─ Agent.Template/
│  ├─ Agent.Core/
│  ├─ Agent.Mcp/
│  ├─ Agent.Providers/
│  ├─ Agent.Aws/
│  └─ Agent.Commander/
│
├─ tests/
│  ├─ Agent.Template.Tests/
│  ├─ Agent.Core.Tests/
│  ├─ Agent.Mcp.Tests/
│  ├─ Agent.Providers.Tests/
│  └─ Agent.Commander.Tests/
│
├─ docker/
│  ├─ Dockerfile
│  ├─ docker-compose.yml
│  ├─ entrypoint.sh
│  └─ healthcheck.sh
│
└─ scripts/
   ├─ bake.sh
   ├─ run-local.sh
   └─ curl/
      └─ chat.sh
```

Notes
- Don’t commit secrets. Supply them via compose/environment.
- One image per config in `configs/`. Baked during build.

---

## Config schema (concept)

Minimal fields (JSON):

```jsonc
{
  "agent": {
    "name": "string",
    "systemPrompt": "string" // persona text
  },
  "model": {
    "provider": "github-models",
    "modelId": "openai/gpt-4o-mini",
    "parameters": {
      "temperature": 0.2,
      "top_p": 1.0
    }
  },
  "mcp": {
    "transport": "streamable-http" // or "stdio",
    "http": {
      "url": "https://example.com/mcp",
      "allowSse": true,
      "timeoutMs": 60000
    },
    "stdio": {
      "command": "npx",
  "args": ["-y", "slite-mcp"],
      "env": {
        "SLITE_API_KEY": "${SLITE_API_KEY}" // provided at runtime
      },
      "workingDir": null
    },
    "session": {
      "useSessionHeader": true,
      "resume": false
    }
  },
  "runtime": {
    "port": 8080
  },
  "security": {
    "validateOrigin": false,
    "apiAuthEnabled": false // placeholders only
  }
}
```

Example (Slite stdio):

```json
{
  "agent": {
    "name": "slite-agent",
    "systemPrompt": "You are a Slite expert. Answer with clear steps and cite note IDs when relevant."
  },
  "model": {
    "provider": "github-models",
    "modelId": "openai/gpt-4o-mini",
    "parameters": { "temperature": 0.2 }
  },
  "mcp": {
    "transport": "stdio",
    "stdio": {
      "command": "npx",
  "args": ["-y", "slite-mcp"],
      "env": { "SLITE_API_KEY": "${SLITE_API_KEY}" }
    }
  },
  "runtime": { "port": 8080 },
  "security": { "validateOrigin": false, "apiAuthEnabled": false }
}
```

Example (New Relic placeholder / Streamable HTTP):

```json
{
  "agent": {
    "name": "newrelic-agent",
    "systemPrompt": "You are a New Relic expert. Provide accurate NRQL and troubleshooting steps."
  },
  "model": {
    "provider": "github-models",
    "modelId": "openai/gpt-4o-mini",
    "parameters": { "temperature": 0.2 }
  },
  "mcp": {
    "transport": "streamable-http",
    "http": {
      "url": "http://newrelic-mcp.local/mcp",
      "allowSse": true,
      "timeoutMs": 60000
    },
    "session": { "useSessionHeader": true }
  },
  "runtime": { "port": 8080 },
  "security": { "validateOrigin": false, "apiAuthEnabled": false }
}
```

Example (Agent Commander):

```json
{
  "agents": [
    {
      "name": "slite",
      "endpoint": "http://localhost:8081/mcp/call",
      "capabilities": ["ask", "search"]
    },
    {
      "name": "newrelic",
      "endpoint": "http://localhost:8082/mcp/call",
      "capabilities": ["monitor", "alert"]
    }
  ]
}
```

---

## Scaffolding instructions (repeatable)

1) Prerequisites
- Install .NET 8 SDK and Docker (with buildx) on the host.
- Ensure QEMU/buildx supports linux/arm64 builds on your host.

2) Create solution and projects
- Create a .NET solution `agent-smith.sln`.
- Add projects:
  - `src/Agent.Template` (ASP.NET Core Minimal API)
  - `src/Agent.Core` (domain/orchestration)
  - `src/Agent.Mcp` (transports + client)
  - `src/Agent.Providers` (GitHub Models, Slite/NewRelic placeholders)
  - `src/Agent.Aws` (future helpers)
  - `src/Agent.Commander` (Agent Commander service)
- Add corresponding test projects under `tests/`.
- Add `Directory.Build.props` with nullable and warnings-as-errors.
- Add `Directory.Packages.props` for central NuGet versions.

3) Implement public HTTP API (v1)
- `POST /v1/chat`: Accept OpenAI-style messages array; use `systemPrompt` from config as a system message; call Reasoner/Provider; return a single JSON object response.
- `GET /healthz`: Return basic health status.
- Add placeholders for bearer auth (disabled) and origin validation (off by default).

4) Implement configuration loading
- Bake a single config JSON into the image under `/app/config/agent.json`.
- Read and validate config at startup (fail-fast if invalid).
- Allow env-var substitution for `${VAR}` placeholders at runtime.

5) Implement MCP client
- Streamable HTTP transport
  - POST all client JSON-RPC messages to MCP endpoint.
  - Include `Accept: application/json, text/event-stream`.
  - Handle JSON responses or SSE event streams on POST.
  - GET “listening” mode: deferred.
  - `Mcp-Session-Id` session header: deferred (config placeholders exist).
- stdio transport
  - Launch child process per config (e.g., `npx -y slite-mcp`).
  - JSON-RPC messages delimited by newlines; handle batches, cancellations, timeouts.
  - Manage process lifecycle and stderr logging.

6) Implement Provider: GitHub Models (LLM)
- Endpoint: `https://models.github.ai/inference/chat/completions`.
- Headers: `Authorization: Bearer ${GITHUB_MODELS_TOKEN}`, `Accept: application/vnd.github+json`, `X-GitHub-Api-Version: 2022-11-28`, `Content-Type: application/json`.
- Use `model.modelId` and `model.parameters` from config.

7) Dockerization (Alpine, linux/arm64)
- Multi-stage Dockerfile: restore/build/publish on `sdk:8.0-alpine`; runtime on `aspnet:8.0-alpine`.
- Install Node/npm in the runtime image (apk add nodejs npm) for Slite stdio.
- Copy the chosen config JSON to `/app/config/agent.json` at build.
- Expose 8080; set `ASPNETCORE_URLS=http://0.0.0.0:8080`.

8) One image per config (bake)
- Provide a script (e.g., `scripts/bake.sh`) or use build args to select config file under `configs/`.
- Tag images based on config name (e.g., `agent-smith:slite`, `agent-smith:newrelic`).

9) docker-compose for local
- Define services for each baked image.
- Map host ports: 8081→8080 (slite), 8082→8080 (newrelic).
- Supply env vars: `SLITE_API_KEY`, `GITHUB_MODELS_TOKEN`.

10) Curl examples
- Non-streaming chat:
  ```bash
  curl -sS -X POST http://localhost:8081/v1/chat \
    -H 'Content-Type: application/json' \
    -d '{
      "messages": [
        {"role":"user","content":"Search Slite for onboarding docs"}
      ]
    }'
  ```

---

## Agent Commander

### Responsibilities
The Agent Commander is a standalone service that acts as the primary interface for users to interact with multiple agents. It accepts natural language input, determines which agents to query, and aggregates responses into a human-readable format. The Agent Commander provides real-time feedback and logs all interactions.

### Architecture
- **Standalone Service**: Implemented as a lightweight ASP.NET Core Minimal API.
- **Agent Registry**: Maintains metadata about available agents (e.g., name, endpoint, capabilities).
- **Concurrency**: Sends requests to agents concurrently to improve performance.
- **Response Aggregation**: Combines responses from multiple agents, generates a summary, and includes detailed responses with metadata.
- **Real-Time Feedback**: Uses Server-Sent Events (SSE) or WebSockets to provide updates to the user.

### Interaction Flow
1. User sends a natural language request to the Agent Commander.
2. The Agent Commander determines which agents to query based on the input.
3. Requests are sent to agents concurrently via `/mcp/call`.
4. Responses are aggregated, and a summary is generated.
5. The user receives real-time feedback during processing.
6. The final response includes both a summary and detailed results with metadata.

### Configuration
- **File**: `configs/commander.agent.json`
- **Example**:
  ```json
  {
    "agents": [
      {
        "name": "slite",
        "endpoint": "http://localhost:8081/mcp/call",
        "capabilities": ["ask", "search"]
      },
      {
        "name": "newrelic",
        "endpoint": "http://localhost:8082/mcp/call",
        "capabilities": ["monitor", "alert"]
      }
    ]
  }
  ```

### Setup Instructions
1. Add the Agent Commander project under `src/Agent.Commander`.
2. Define the agent registry and populate it by querying `/mcp/tools` at startup.
3. Implement natural language processing to determine which agents to query.
4. Implement concurrent requests to agents and response aggregation.
5. Add real-time feedback using SSE or WebSockets.
6. Write unit, integration, and end-to-end tests.
7. Update documentation to include the Agent Commander.

---

## Security & compliance notes

- Do not commit secrets. Always pass tokens/keys via env or secret managers.
- MCP Streamable HTTP security guidance recommends Origin checks and localhost binding for local servers. Placeholders are included; enable when needed.
- SSE support is implemented for MCP compliance (POST responses; GET listening deferred). The agent’s public API remains non-streaming in v1.

---

## Port and image naming (defaults)

- Ports
  - `slite` image: host 8081 → container 8080
  - `newrelic` image: host 8082 → container 8080

- Images
  - `agent-smith:slite`
  - `agent-smith:newrelic`

---

## Checklist for a new agent config

1) Create `configs/<name>.agent.json` with:
   - Agent persona (`systemPrompt`)
   - Model (`modelId`, parameters)
   - MCP transport (`streamable-http` or `stdio`) and settings
   - Runtime port (default 8080)
2) Build image tagged with `<name>` (bake process copies your config into the image)
3) Provide required env vars via compose or `docker run` (e.g., `SLITE_API_KEY`, `GITHUB_MODELS_TOKEN`)
4) Run and test with curl on the mapped host port

---

## What’s intentionally deferred

- Agent API streaming (NDJSON/SSE) for client responses
- Metrics (Prometheus) and tracing (OpenTelemetry)
- AWS ECS/ECR helpers wiring (kept as a separate project, no infra changes yet)

---

## Troubleshooting notes

- stdio server not starting: confirm Node is present in the container; verify `npx -y slite-mcp` works; check stderr logs from the child process.
- MCP Streamable HTTP returns SSE unexpectedly: validate Accept headers and ensure SSE parsing is active; verify server endpoint is correct.
- GitHub Models 401/403: ensure `GITHUB_MODELS_TOKEN` has `models` scope; check HTTP headers.
- Timeouts: adjust `mcp.http.timeoutMs` or stdio timeouts in client.
