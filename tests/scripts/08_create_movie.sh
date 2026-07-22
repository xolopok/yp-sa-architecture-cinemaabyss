#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8080"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/movies" \
    -H "Content-Type: application/json" \
    -d '{
        "title": "Test Movie '$(date +%s)'",
        "description": "A test movie created via script",
        "genres": ["Action", "Drama"],
        "rating": 4.5
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
