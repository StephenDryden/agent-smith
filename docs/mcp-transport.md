# MCP transport behavior

- Streamable HTTP
  - POST with Accept: application/json, text/event-stream; handle JSON or SSE responses.
  - GET listening with Accept: text/event-stream — deferred (not implemented yet).
  - Mcp-Session-Id session header — deferred (placeholders only).

- stdio
  - Launch child process (e.g., npx -y slite-mcp), communicate via newline-delimited JSON-RPC.
  - Handle batches, cancellations, timeouts; capture stderr.
