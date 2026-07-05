#!/bin/bash
# scripts/smoke-tests.sh
# Post-deployment validation - runs critical tests to verify deployment success

set -e

API_URL=${1:-http://localhost:8080}
TIMEOUT=30
FAILED=0

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "==========================================="
echo "🔍 Running Smoke Tests"
echo "==========================================="
echo "Target: $API_URL"
echo "Timeout: ${TIMEOUT}s"
echo ""

# Test 1: Liveness Probe
echo -n "📍 Testing liveness probe... "
if curl -sf "$API_URL/health/live" --connect-timeout $TIMEOUT > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
fi

# Test 2: Readiness Probe
echo -n "📍 Testing readiness probe... "
if curl -sf "$API_URL/health/ready" --connect-timeout $TIMEOUT > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
fi

# Test 3: Health Details
echo -n "📍 Testing detailed health... "
HEALTH=$(curl -sf "$API_URL/health/detailed" --connect-timeout $TIMEOUT 2>/dev/null)
if [ $? -eq 0 ]; then
    DB_STATUS=$(echo "$HEALTH" | jq -r '.checks[] | select(.name=="database") | .status' 2>/dev/null)
    CACHE_STATUS=$(echo "$HEALTH" | jq -r '.checks[] | select(.name=="cache") | .status' 2>/dev/null)
    
    if [ "$DB_STATUS" = "healthy" ] && [ "$CACHE_STATUS" = "healthy" ]; then
        echo -e "${GREEN}✓ PASS${NC}"
    else
        echo -e "${RED}✗ FAIL (DB: $DB_STATUS, Cache: $CACHE_STATUS)${NC}"
        FAILED=$((FAILED + 1))
    fi
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
fi

# Test 4: API Response Format
echo -n "📍 Testing API response format... "
RESPONSE=$(curl -sf "$API_URL/api/v1/orders" \
    -H "Content-Type: application/json" \
    -H "Accept: application/json" \
    --connect-timeout $TIMEOUT 2>/dev/null)

if [ $? -eq 0 ] && echo "$RESPONSE" | jq empty 2>/dev/null; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${RED}✗ FAIL${NC}"
    FAILED=$((FAILED + 1))
fi

# Test 5: Response Headers
echo -n "📍 Testing security headers... "
HEADERS=$(curl -si "$API_URL/health/live" --connect-timeout $TIMEOUT 2>/dev/null)
HSTS=$(echo "$HEADERS" | grep -i "strict-transport-security" || true)
CSP=$(echo "$HEADERS" | grep -i "content-security-policy" || true)

if [ -n "$HSTS" ] && [ -n "$CSP" ]; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${YELLOW}⚠ PARTIAL (missing security headers)${NC}"
fi

# Test 6: Metrics Endpoint
echo -n "📍 Testing metrics endpoint... "
if curl -sf "$API_URL/metrics" --connect-timeout $TIMEOUT > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PASS${NC}"
else
    echo -e "${YELLOW}⚠ NOT AVAILABLE (optional)${NC}"
fi

# Test 7: Response Time
echo -n "📍 Testing response time... "
START=$(date +%s%N)
curl -sf "$API_URL/health/live" --connect-timeout $TIMEOUT > /dev/null 2>&1
END=$(date +%s%N)
ELAPSED=$((($END - $START) / 1000000))

if [ $ELAPSED -lt 1000 ]; then
    echo -e "${GREEN}✓ PASS (${ELAPSED}ms)${NC}"
else
    echo -e "${YELLOW}⚠ SLOW (${ELAPSED}ms > 1000ms target)${NC}"
fi

# Summary
echo ""
echo "==========================================="
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ All smoke tests passed${NC}"
    echo "==========================================="
    exit 0
else
    echo -e "${RED}❌ $FAILED test(s) failed${NC}"
    echo "==========================================="
    exit 1
fi
