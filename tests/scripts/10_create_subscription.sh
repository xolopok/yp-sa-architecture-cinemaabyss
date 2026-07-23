#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8080"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/subscriptions" \
    -H "Content-Type: application/json" \
    -d '{
        "user_id": 1,
        "plan_type": "premium",
        "start_date": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
        "end_date": "'$(date -u -d '+30 days' +%Y-%m-%dT%H:%M:%SZ)'"
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
