# MCP transport behavior

- Streamable HTTP
  - POST with Accept: application/json, text/event-stream; handle JSON or SSE responses.
  - Optional GET listening with Accept: text/event-stream (v1 supported).
  - Mcp-Session-Id header supported.

- stdio
  - Launch child process (e.g., npx -y slite-mcp), communicate via newline-delimited JSON-RPC.
  - Handle batches, cancellations, timeouts; capture stderr.
