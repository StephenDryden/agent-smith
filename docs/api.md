# Agent HTTP API (v1)

- POST /v1/chat
  - Request: { messages: [{ role: "user"|"system"|"assistant", content: string }] }
  - Response: { message: { role: "assistant", content: string } }
  - v1 is non-streaming.

- GET /healthz
  - Response: { status: "healthy" }
