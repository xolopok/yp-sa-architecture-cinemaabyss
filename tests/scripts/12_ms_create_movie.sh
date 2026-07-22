#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8081"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/movies" \
    -H "Content-Type: application/json" \
    -d '{
        "title": "MS Test Movie '$(date +%s)'",
        "description": "A test movie for movies microservice",
        "genres": ["Sci-Fi", "Thriller"],
        "rating": 4.8
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
