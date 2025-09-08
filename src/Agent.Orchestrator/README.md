# Agent Orchestrator

The Agent Orchestrator is a service that interacts with multiple agents, aggregates their responses, and provides real-time feedback.

## Building and Running

	docker build -f docker/Dockerfile.agent-orchestrator -t agent-smith:orchestrator .
	docker run -p 8083:8080 agent-smith:orchestrator

1. Ensure the `docker-compose.yml` file includes the `agent-orchestrator` service.

The `Agent.Orchestrator` reads its configuration from `configs/orchestrator.agent.json`. Ensure this file is correctly set up before running the service.
