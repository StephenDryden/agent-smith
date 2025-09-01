# Architecture (overview)

- Agent.Template: ASP.NET Core Minimal API host exposing /v1/chat and /healthz.
- Agent.Core: Agent orchestration, config models, and chat DTOs.
- Agent.Mcp: MCP transports (Streamable HTTP with SSE and stdio) and client.
- Agent.Providers: Integrations (GitHub Models provider; Slite/NewRelic placeholders).

Data flow (v1):
client -> Agent.Template (/v1/chat) -> Core (compose persona + request) -> Provider (GitHub Models) -> response

Future: MCP client used for tool calls and external server interactions.
