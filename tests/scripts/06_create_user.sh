#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8080"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/users" \
    -H "Content-Type: application/json" \
    -d '{
        "username": "testuser_'$(date +%s)'",
        "email": "testuser_'$(date +%s)'@example.com"
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
