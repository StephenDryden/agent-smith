# Local development

- Build: dotnet build
- Run Template project for local dev
- Use docker-compose for multi-agent; ports: 8081 (slite), 8082 (newrelic)
- Provide env vars: SLITE_API_KEY, GITHUB_MODELS_TOKEN
- Test: see scripts/curl/chat.sh
