#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8082"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/events/payment" \
    -H "Content-Type: application/json" \
    -d '{
        "payment_id": 1,
        "user_id": 1,
        "amount": 9.99,
        "status": "completed",
        "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",
        "method_type": "credit_card"
    }')

if command -v jq >/dev/null 2>&1; then
    echo "$RESPONSE" | jq .
else
    echo "$RESPONSE"
fi
