#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8082"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/events/movie" \
    -H "Content-Type: application/json" \
    -d '{
        "movie_id": 1,
        "title": "Test Movie Event",
        "action": "viewed",
        "user_id": 1
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
