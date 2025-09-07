# Agent Commander

The Agent Commander is a service that interacts with multiple agents, aggregates their responses, and provides real-time feedback.

## Features
- Health check endpoint: `/health`
- Endpoint to list discovered agents and their tools: `/agents`
- Logs discovered agents and tools at startup.

## Running the Service

### Using Docker
1. Build the Docker image:
   ```bash
   docker build -f docker/Dockerfile.agent-commander -t agent-smith:commander .
   ```
2. Run the container:
   ```bash
   docker run -p 8083:8080 agent-smith:commander
   ```

### Using Docker Compose
1. Ensure the `docker-compose.yml` file includes the `agent-commander` service.
2. Start the services:
   ```bash
   docker-compose up
   ```

## Endpoints
- **Health Check**: `GET /health`
- **List Agents**: `GET /agents`

## Configuration
The `Agent.Commander` reads its configuration from `configs/commander.agent.json`. Ensure this file is correctly set up before running the service.
