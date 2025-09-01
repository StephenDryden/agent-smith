Configs

This folder contains per-agent JSON configurations consumed by the service at startup.

Notes:
- Supports `${VAR}` env substitution at runtime; ensure secrets are present in `.env` and passed to containers via `env_file`.
- `model.provider` can be overridden by `MODEL_PROVIDER` env var.
- `mcp.transport` may be `stdio` or `streamable-http`.

Add a new agent:
1) Copy an existing `.agent.json` and adjust `agent`, `model`, `mcp`, and `runtime`.
2) Add a compose service using `build.args.CONFIG_FILE` to bake your config.
3) Rebuild with docker compose and test `/healthz` and `/v1/chat`.
