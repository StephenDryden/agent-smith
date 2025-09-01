Agent.Providers

Purpose: Model provider abstraction and implementations.

Includes:
- `IModelProvider` interface: `GetChatCompletionAsync` for non-streaming chat
- `GitHubModelsProvider`: calls https://models.github.ai/inference/chat/completions with proper headers and parses `choices[0].message.content`
- `MockModelsProvider`: returns deterministic mock content for local/offline tests

Configuration:
- Default provider is `github-models` (overridable via `MODEL_PROVIDER`)
- Credentials via env (`GITHUB_MODELS_TOKEN`); ensure present in `.env` for real requests
