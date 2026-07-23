#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8080"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/payments" \
    -H "Content-Type: application/json" \
    -d '{
        "user_id": 1,
        "amount": 9.99
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
