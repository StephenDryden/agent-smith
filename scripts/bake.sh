#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT_DIR"

# Build two images by default using the same Dockerfile and mount config at runtime via compose
# For true bake (copy-in), you can add separate stages or ARG to copy configs. Keeping simple with compose bind for now.

echo "Building base image for agent (Alpine arm64)..."
docker buildx build \
  --platform linux/arm64 \
  -t agent-smith:base \
  -f docker/Dockerfile .

echo "Images will be run via docker-compose with mounted configs."
