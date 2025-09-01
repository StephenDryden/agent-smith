# Config schema (concept)

See blueprint.md for full schema examples. Key sections:
- agent: name, systemPrompt
- model: provider, modelId, parameters
- mcp: transport (streamable-http|stdio) and respective settings
- runtime: port
- security: placeholders for origin validation and API auth (off by default)
