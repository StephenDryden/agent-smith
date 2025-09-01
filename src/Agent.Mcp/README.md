Agent.Mcp

Purpose: Client and transports for Model Context Protocol (MCP).

Components:
- McpClient: selects transport from config and orchestrates initialize/send/receive
- Transports:
  - StdioTransport: launches a process and communicates via stdin/stdout (newline-delimited JSON). Read loop is cancellable to avoid hangs.
  - StreamableHttpTransport: connects to an HTTP/SSE MCP endpoint
- Protocol types: minimal JSON-RPC request/response/notification structures

Usage:
- `Agent.Template` constructs `McpClient` with the active agent config
- On chat, the app may initialize MCP and drain a few messages before model inference
