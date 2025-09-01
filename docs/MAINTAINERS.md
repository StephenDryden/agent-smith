# Maintainer Notes

This repo contains a C# agent template targeting .NET 8 on Alpine arm64, producing per-config images.

Key entry points
- Agent.Template/Program.cs: Minimal API host, config loading, /v1/chat and /healthz.
- Agent.Core/Config.cs: Config schema and chat DTOs.
- Agent.Mcp: MCP client and transports
  - Transport/StreamableHttpTransport.cs: Streamable HTTP + SSE (skeleton)
  - Transport/StdioTransport.cs: stdio subprocess transport (skeleton)
  - Clients/McpClient.cs: Chooses transport from config
- configs/*.agent.json: One image per config; baked or mounted via compose.
- docker/: Dockerfile, compose, entrypoint, healthcheck.
- scripts/: bake, run-local, curl examples.

Conventions
- No generic filenames like Class1.cs; use descriptive names.
- Keep secrets out of source; use env vars.
- Agent API is non-streaming in v1; SSE only for MCP.

Future work
- Implement full JSON-RPC/MCP request/response handling and SSE parsing.
- Add GitHub Models provider and real reasoning.
- Add tests and CI for transports and providers.
