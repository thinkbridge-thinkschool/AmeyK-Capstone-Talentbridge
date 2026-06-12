#!/bin/bash
BASE_URL="https://localhost:7001"

echo "=== TalentBridge Circuit Breaker Test ==="

echo "Step 1: Enable forced failures"
curl -k -X POST "$BASE_URL/api/resilience/force-failure/true"
echo ""

echo "Step 2: Fire 8 requests — watch retries then circuit open"
for i in {1..8}; do curl -k -s "$BASE_URL/api/resilience/test-call" & done
wait
echo ""

echo "Step 3: Verify circuit is open — should fail instantly"
curl -k "$BASE_URL/api/resilience/test-call"
echo ""

echo "Step 4: Check status"
curl -k "$BASE_URL/api/resilience/status"
echo ""

echo "Step 5: Disable failures"
curl -k -X POST "$BASE_URL/api/resilience/force-failure/false"
echo ""

echo "Step 6: Wait 30s for half-open"
sleep 30

echo "Step 7: Recovery test"
curl -k "$BASE_URL/api/resilience/test-call"
echo ""
echo "=== Test complete ==="
