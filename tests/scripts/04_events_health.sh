#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8082"

RESPONSE=$(curl -s -X GET "$BASE_URL/api/events/health")

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
