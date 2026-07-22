#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8082"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/events/user" \
    -H "Content-Type: application/json" \
    -d '{
        "user_id": 1,
        "username": "testuser",
        "action": "logged_in",
        "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
