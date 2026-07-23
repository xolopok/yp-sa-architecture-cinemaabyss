#!/usr/bin/env bash
set -euo pipefail

BASE_URL="http://localhost:8000"

RESPONSE=$(curl -s -X GET "$BASE_URL/health")

echo "$RESPONSE"
