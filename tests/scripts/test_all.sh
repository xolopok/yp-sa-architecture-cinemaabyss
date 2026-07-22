#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PASS=0
FAIL=0
RESULTS=()

run_test() {
    local name="$1"
    local method="$2"
    local url="$3"
    local data="$4"

    local curl_args=(-s -o /tmp/cinemaabyss_test_response -w "%{http_code}" -X "$method")
    if [ -n "$data" ]; then
        curl_args+=(-H "Content-Type: application/json" -d "$data")
    fi
    curl_args+=("$url")

    local http_code
    http_code=$(curl "${curl_args[@]}")

    local passed=false
    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        passed=true
    fi

    local body
    body=$(cat /tmp/cinemaabyss_test_response)

    local mark
    if $passed; then
        mark="PASS"
        PASS=$((PASS + 1))
    else
        mark="FAIL"
        FAIL=$((FAIL + 1))
    fi

    RESULTS+=("$mark | $http_code | $name")

    echo "--- $name ---"
    if command -v jq >/dev/null 2>&1 && echo "$body" | jq . >/dev/null 2>&1; then
        echo "$body" | jq .
    else
        echo "$body"
    fi
    echo "[$mark] HTTP $http_code"
    echo ""
}

echo "========================================="
echo "  CinemaAbyss API Test Suite"
echo "========================================="
echo ""

# ---- Health checks ----
run_test "01 Monolith Health"          "GET"  "http://localhost:8080/health"                      ""
run_test "02 Movies MS Health"         "GET"  "http://localhost:8081/api/movies/health"             ""
run_test "03 Proxy Health"             "GET"  "http://localhost:8000/health"                        ""
run_test "04 Events Health"            "GET"  "http://localhost:8082/api/events/health"             ""

# ---- Monolith ----
run_test "05 Get Users (monolith)"     "GET"  "http://localhost:8080/api/users"                    ""
run_test "06 Create User"              "POST" "http://localhost:8080/api/users"                     '{"username":"testuser","email":"testuser@example.com"}'
run_test "07 Get Movies (monolith)"    "GET"  "http://localhost:8080/api/movies"                    ""
run_test "08 Create Movie"             "POST" "http://localhost:8080/api/movies"                    '{"title":"Test Movie","description":"A test movie","genres":["Action","Drama"],"rating":4.5}'
run_test "09 Create Payment"           "POST" "http://localhost:8080/api/payments"                  '{"user_id":1,"amount":9.99}'
run_test "10 Create Subscription"      "POST" "http://localhost:8080/api/subscriptions"             '{"user_id":1,"plan_type":"premium","start_date":"2025-01-01T00:00:00Z","end_date":"2025-12-31T00:00:00Z"}'

# ---- Movies Microservice ----
run_test "11 MS Get Movies"            "GET"  "http://localhost:8081/api/movies"                    ""
run_test "12 MS Create Movie"          "POST" "http://localhost:8081/api/movies"                    '{"title":"MS Test","description":"Microservice test","genres":["Sci-Fi"],"rating":4.8}'

# ---- Proxy ----
run_test "13 Proxy Get Movies"         "GET"  "http://localhost:8000/api/movies"                    ""
run_test "14 Proxy Get Users"          "GET"  "http://localhost:8000/api/users"                     ""

# ---- Events ----
run_test "15 Create Movie Event"       "POST" "http://localhost:8082/api/events/movie"              '{"movie_id":1,"title":"Test Movie","action":"viewed","user_id":1}'
run_test "16 Create User Event"        "POST" "http://localhost:8082/api/events/user"               '{"user_id":1,"username":"testuser","action":"logged_in","timestamp":"2025-01-01T00:00:00Z"}'
run_test "17 Create Payment Event"     "POST" "http://localhost:8082/api/events/payment"            '{"payment_id":1,"user_id":1,"amount":9.99,"status":"completed","timestamp":"2025-01-01T00:00:00Z","method_type":"credit_card"}'

echo "========================================="
echo "  Summary"
echo "========================================="
printf "%-6s | %-4s | %s\n" "Status" "Code" "Test"
printf "%-6s-+-%-4s-+-%s\n" "------" "----" "$(printf '%.0s-' {1..40})"
for result in "${RESULTS[@]}"; do
    echo "$result"
done
echo "========================================="
echo "  PASS: $PASS  FAIL: $FAIL  TOTAL: $((PASS + FAIL))"
echo "========================================="

if [ "$FAIL" -gt 0 ]; then
    exit 1
fi
