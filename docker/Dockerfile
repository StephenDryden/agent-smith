# syntax=docker/dockerfile:1.7-labs

ARG TARGETARCH=arm64
ARG SDK_TAG=8.0-alpine
ARG RUNTIME_TAG=8.0-alpine

FROM mcr.microsoft.com/dotnet/sdk:${SDK_TAG} AS build
WORKDIR /src

# restore
COPY agent-smith.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY nuget.config ./
COPY src/Agent.Core/Agent.Core.csproj src/Agent.Core/
COPY src/Agent.Mcp/Agent.Mcp.csproj src/Agent.Mcp/
COPY src/Agent.Providers/Agent.Providers.csproj src/Agent.Providers/
COPY src/Agent.Template/Agent.Template.csproj src/Agent.Template/
RUN dotnet restore src/Agent.Template/Agent.Template.csproj

# build
COPY . ./
RUN dotnet publish src/Agent.Template/Agent.Template.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:${RUNTIME_TAG} AS runtime
ARG TARGETARCH
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
WORKDIR /app

# install Node/npm for Slite stdio
RUN apk add --no-cache nodejs npm

# copy app
COPY --from=build /app/publish/ ./

# Optional baked config
ARG CONFIG_FILE=""
RUN mkdir -p /app/config
COPY ${CONFIG_FILE} /app/config/agent.json

# baked config will be copied at build time via --build-arg or separate stage; default path
ENV AGENT_CONFIG_PATH=/app/config/agent.json

EXPOSE 8080

# healthcheck script will be added via separate COPY in compose or by rebuild with files
ENTRYPOINT ["/app/Agent.Template"]
