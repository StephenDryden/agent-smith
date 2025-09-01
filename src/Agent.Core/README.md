Agent.Core

Purpose: Shared types and configuration primitives used by the agent.

Includes:
- AgentConfig (agent/systemPrompt, model, mcp, runtime, security)
- Chat DTOs (ChatMessage, ChatRequest, ChatResponse)
- Simple env substitution utility for `${VAR}` in JSON configs

Consumers:
- Used by `Agent.Template` for config loading and request handling
- Referenced by provider and MCP projects for common types
