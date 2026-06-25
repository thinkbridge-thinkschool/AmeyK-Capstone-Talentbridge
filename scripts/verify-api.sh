#!/usr/bin/env bash
# TalentBridge API — Happy-Path Verification
# Usage:  bash scripts/verify-api.sh [BASE_URL]
# Default: http://localhost:5000
#
# Requires: curl, python3
# Run the backend first:  cd src/API/TalentBridge.API && dotnet run

set -euo pipefail
BASE="${1:-http://localhost:5000}"

# ── Helpers ───────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'; RED='\033[0;31m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; NC='\033[0m'
PASS=0; FAIL=0

pass()    { echo -e "${GREEN}[PASS]${NC} $1"; ((PASS++)); }
fail()    { echo -e "${RED}[FAIL]${NC} $1"; ((FAIL++)); }
section() { echo -e "\n${YELLOW}══════ $1 ══════${NC}"; }
info()    { echo -e "${CYAN}  ▶${NC} $1"; }

# POST helper: returns response body; exits with status for checks
post() { curl -sf -X POST "$BASE$1" -H "Content-Type: application/json" ${3:+-H "Authorization: Bearer $3"} -d "$2"; }
get()  { curl -sf -X GET  "$BASE$1" ${2:+-H "Authorization: Bearer $2"}; }
patch(){ curl -sf -X PATCH "$BASE$1" -H "Content-Type: application/json" ${3:+-H "Authorization: Bearer $3"} -d "${2:-{}}"; }
put()  { curl -sf -X PUT  "$BASE$1" -H "Content-Type: application/json" ${3:+-H "Authorization: Bearer $3"} -d "$2"; }
del()  { curl -sf -X DELETE "$BASE$1" ${2:+-H "Authorization: Bearer $2"}; }
json() { python3 -c "import sys,json; d=json.load(sys.stdin); print(d$1)" 2>/dev/null; }

check_http() {
  local label="$1" url="$2" method="${3:-GET}" token="${4:-}" body="${5:-}"
  local status
  if [[ "$method" == "GET" ]]; then
    status=$(curl -so /dev/null -w "%{http_code}" -H "Authorization: Bearer $token" "$BASE$url")
  else
    status=$(curl -so /dev/null -w "%{http_code}" -X "$method" "$BASE$url" \
      -H "Content-Type: application/json" -H "Authorization: Bearer $token" -d "$body")
  fi
  if [[ "$status" -ge 200 && "$status" -lt 300 ]]; then
    pass "$label (HTTP $status)"
  else
    fail "$label (HTTP $status)"
  fi
}

echo -e "${CYAN}"
echo "  ████████╗ █████╗ ██╗     ███████╗███╗   ██╗████████╗██████╗ ██████╗ ██╗██████╗  ██████╗ ███████╗"
echo "     ██╔══╝██╔══██╗██║     ██╔════╝████╗  ██║╚══██╔══╝██╔══██╗██╔══██╗██║██╔══██╗██╔════╝ ██╔════╝"
echo "     ██║   ███████║██║     █████╗  ██╔██╗ ██║   ██║   ██████╔╝██████╔╝██║██║  ██║██║  ███╗█████╗  "
echo "     ██║   ██╔══██║██║     ██╔══╝  ██║╚██╗██║   ██║   ██╔══██╗██╔══██╗██║██║  ██║██║   ██║██╔══╝  "
echo "     ██║   ██║  ██║███████╗███████╗██║ ╚████║   ██║   ██████╔╝██║  ██║██║██████╔╝╚██████╔╝███████╗"
echo "     ╚═╝   ╚═╝  ╚═╝╚══════╝╚══════╝╚═╝  ╚═══╝   ╚═╝   ╚═════╝ ╚═╝  ╚═╝╚═╝╚═════╝  ╚═════╝ ╚══════╝"
echo -e "${NC}"
echo "  API Happy-Path Verification  ·  $BASE"
echo "  $(date '+%Y-%m-%d %H:%M:%S')"

# ─────────────────────────────────────────────────────────────────────────────
section "1 · IDENTITY — Login & Token"
# ─────────────────────────────────────────────────────────────────────────────

# Login as Admin
info "Admin login"
ADMIN_RESP=$(post "/api/identity/login" '{"email":"admin@talentbridge.com","password":"Admin@1234"}')
ADMIN_TOKEN=$(echo "$ADMIN_RESP" | json "['token']")
ADMIN_REFRESH=$(echo "$ADMIN_RESP" | json "['refreshToken']")
[[ -n "$ADMIN_TOKEN" ]] && pass "Admin login → token received" || fail "Admin login"

# Login as HR
info "HR login"
HR_RESP=$(post "/api/identity/login" '{"email":"hr@talentbridge.com","password":"HR@1234"}')
HR_TOKEN=$(echo "$HR_RESP" | json "['token']")
HR_REFRESH=$(echo "$HR_RESP" | json "['refreshToken']")
[[ -n "$HR_TOKEN" ]] && pass "HR login → token received" || fail "HR login"

# Login as Candidate
info "Candidate login"
CAND_RESP=$(post "/api/identity/login" '{"email":"candidate@talentbridge.com","password":"Candidate@1234"}')
CAND_TOKEN=$(echo "$CAND_RESP" | json "['token']")
CAND_REFRESH=$(echo "$CAND_RESP" | json "['refreshToken']")
[[ -n "$CAND_TOKEN" ]] && pass "Candidate login → token received" || fail "Candidate login"

# GET /api/identity/me
info "GET /api/identity/me (Admin)"
ME=$(get "/api/identity/me" "$ADMIN_TOKEN")
ME_EMAIL=$(echo "$ME" | json "['email']")
[[ "$ME_EMAIL" == "admin@talentbridge.com" ]] && pass "GET /me → correct email" || fail "GET /me (got: $ME_EMAIL)"

# Refresh token
info "POST /api/identity/refresh"
REFRESH_RESP=$(post "/api/identity/refresh" "{\"refreshToken\":\"$CAND_REFRESH\"}")
NEW_TOKEN=$(echo "$REFRESH_RESP" | json "['accessToken']")
[[ -n "$NEW_TOKEN" ]] && pass "Token refresh → new access token received" || fail "Token refresh"
CAND_TOKEN="$NEW_TOKEN"   # use the rotated token from here on

# ─────────────────────────────────────────────────────────────────────────────
section "2 · IDENTITY — Register"
# ─────────────────────────────────────────────────────────────────────────────

REG_EMAIL="testuser_$(date +%s)@example.com"
info "POST /api/identity/register"
REG_RESP=$(post "/api/identity/register" \
  "{\"email\":\"$REG_EMAIL\",\"password\":\"Test@123456\",\"role\":\"Candidate\",\"fullName\":\"Test User\"}" \
  || echo "{}")
REG_ID=$(echo "$REG_RESP" | json "['userId']" 2>/dev/null || echo "")
[[ -n "$REG_ID" ]] && pass "Register new candidate → userId returned" || fail "Register new candidate"

# ─────────────────────────────────────────────────────────────────────────────
section "3 · JOBS — Browse & Search"
# ─────────────────────────────────────────────────────────────────────────────

info "GET /api/jobs/search"
JOBS_RESP=$(get "/api/jobs/search?page=1&size=10")
JOB_COUNT=$(echo "$JOBS_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d) if isinstance(d,list) else d.get('totalCount',len(d.get('items',[]))))" 2>/dev/null || echo "0")
[[ "$JOB_COUNT" -ge 3 ]] && pass "GET /api/jobs/search → $JOB_COUNT jobs returned" || fail "GET /api/jobs/search (got $JOB_COUNT)"

info "GET /api/jobs/search?keyword=.NET"
SEARCH_RESP=$(get "/api/jobs/search?keyword=.NET&page=1&size=10")
SEARCH_COUNT=$(echo "$SEARCH_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d) if isinstance(d,list) else len(d.get('items',[])))" 2>/dev/null || echo "0")
[[ "$SEARCH_COUNT" -ge 1 ]] && pass "GET /api/jobs/search?keyword=.NET → match found" || fail "Search by keyword .NET"

# Extract first job ID
JOB_ID=$(echo "$JOBS_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d if isinstance(d,list) else d.get('items',[]); print(items[0]['id'])" 2>/dev/null || echo "")

info "GET /api/jobs/{id}"
if [[ -n "$JOB_ID" ]]; then
  JOB_DETAIL=$(get "/api/jobs/$JOB_ID")
  JOB_TITLE=$(echo "$JOB_DETAIL" | json "['title']" 2>/dev/null || echo "")
  [[ -n "$JOB_TITLE" ]] && pass "GET /api/jobs/$JOB_ID → \"$JOB_TITLE\"" || fail "GET /api/jobs/{id}"
else
  fail "GET /api/jobs/{id} — no job ID available"
fi

# ─────────────────────────────────────────────────────────────────────────────
section "4 · JOBS — HR Post & Manage"
# ─────────────────────────────────────────────────────────────────────────────

HR_ID=$(echo "$HR_RESP" | json "['userId']" 2>/dev/null || \
  python3 -c "
import base64, json, sys
t='$HR_TOKEN'.split('.')[1]
t += '=' * (4 - len(t) % 4)
d=json.loads(base64.b64decode(t))
print(d.get('sub','') or d.get('nameid',''))
" 2>/dev/null || echo "")
COMPANY_ID=$(echo "$JOBS_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d if isinstance(d,list) else d.get('items',[]); print(items[0].get('companyId',''))" 2>/dev/null || echo "")

info "POST /api/jobs (create draft)"
NEW_JOB_RESP=$(post "/api/jobs" \
  "{\"title\":\"QA Automation Engineer\",\"description\":\"Build and maintain automated test suites for enterprise applications using Playwright and SpecFlow with BDD.\",\"location\":\"Hyderabad, India\",\"salaryMin\":50000,\"salaryMax\":80000,\"companyId\":\"$COMPANY_ID\",\"postedByHRId\":\"$HR_ID\"}" \
  "$HR_TOKEN" || echo "{}")
NEW_JOB_ID=$(echo "$NEW_JOB_RESP" | json "['jobId']" 2>/dev/null || echo "$NEW_JOB_RESP" | json "['id']" 2>/dev/null || echo "")
[[ -n "$NEW_JOB_ID" ]] && pass "POST /api/jobs → jobId: $NEW_JOB_ID" || fail "POST /api/jobs"

if [[ -n "$NEW_JOB_ID" ]]; then
  info "GET /api/jobs/mine"
  MINE_RESP=$(get "/api/jobs/mine" "$HR_TOKEN")
  MINE_COUNT=$(echo "$MINE_RESP" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
  [[ "$MINE_COUNT" -ge 1 ]] && pass "GET /api/jobs/mine → $MINE_COUNT job(s)" || fail "GET /api/jobs/mine"

  info "PUT /api/jobs/{id} (update)"
  check_http "PUT /api/jobs/$NEW_JOB_ID" "/api/jobs/$NEW_JOB_ID" "PUT" "$HR_TOKEN" \
    '{"title":"QA Automation Engineer (Updated)","description":"Build and maintain automated test suites for enterprise applications using Playwright and SpecFlow with BDD. Updated.","location":"Hyderabad, India","salaryMin":55000,"salaryMax":85000}'

  info "POST /api/jobs/{id}/publish"
  check_http "POST /api/jobs/$NEW_JOB_ID/publish" "/api/jobs/$NEW_JOB_ID/publish" "POST" "$HR_TOKEN" "{}"

  info "PATCH /api/jobs/{id}/close"
  check_http "PATCH /api/jobs/$NEW_JOB_ID/close" "/api/jobs/$NEW_JOB_ID/close" "PATCH" "$HR_TOKEN" "{}"

  info "DELETE /api/jobs/{id}"
  check_http "DELETE /api/jobs/$NEW_JOB_ID" "/api/jobs/$NEW_JOB_ID" "DELETE" "$HR_TOKEN"
fi

# ─────────────────────────────────────────────────────────────────────────────
section "5 · APPLICATIONS — Candidate Flow"
# ─────────────────────────────────────────────────────────────────────────────

CAND_ID=$(python3 -c "
import base64, json
t='$CAND_TOKEN'.split('.')[1]
t += '=' * (4 - len(t) % 4)
d=json.loads(base64.b64decode(t))
print(d.get('sub','') or d.get('nameid',''))
" 2>/dev/null || echo "")

# Use job3 (DevOps — seeded, published, no existing application)
JOB3_ID=$(echo "$JOBS_RESP" | python3 -c "import sys,json; d=json.load(sys.stdin); items=d if isinstance(d,list) else d.get('items',[]); [print(j['id']) for j in items if 'DevOps' in j.get('title','')]" 2>/dev/null | head -1 || echo "")
[[ -z "$JOB3_ID" ]] && JOB3_ID="$JOB_ID"   # fallback

info "POST /api/applications (apply)"
APPLY_RESP=$(post "/api/applications" \
  "{\"candidateId\":\"$CAND_ID\",\"jobId\":\"$JOB3_ID\",\"coverLetter\":\"I am passionate about cloud DevOps and have 4 years of Azure experience including AKS, Bicep, and GitHub Actions.\",\"resumeUrl\":\"https://example.com/resume.pdf\"}" \
  "$CAND_TOKEN" || echo "{}")
APP_ID=$(echo "$APPLY_RESP" | json "['applicationId']" 2>/dev/null || echo "")
[[ -n "$APP_ID" ]] && pass "POST /api/applications → applicationId: $APP_ID" || fail "POST /api/applications"

info "GET /api/applications/my"
MY_APPS=$(get "/api/applications/my" "$CAND_TOKEN")
MY_COUNT=$(echo "$MY_APPS" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
[[ "$MY_COUNT" -ge 1 ]] && pass "GET /api/applications/my → $MY_COUNT application(s)" || fail "GET /api/applications/my"

if [[ -n "$APP_ID" ]]; then
  info "GET /api/applications/{id}"
  APP_DETAIL=$(get "/api/applications/$APP_ID" "$CAND_TOKEN")
  APP_STATUS=$(echo "$APP_DETAIL" | json "['status']" 2>/dev/null || echo "")
  [[ -n "$APP_STATUS" ]] && pass "GET /api/applications/$APP_ID → status: $APP_STATUS" || fail "GET /api/applications/{id}"

  info "PATCH /api/applications/{id}/withdraw"
  check_http "PATCH /api/applications/$APP_ID/withdraw" "/api/applications/$APP_ID/withdraw" "PATCH" "$CAND_TOKEN" "{}"
fi

# ─────────────────────────────────────────────────────────────────────────────
section "6 · APPLICATIONS — HR Review Flow"
# ─────────────────────────────────────────────────────────────────────────────

# Get first seeded application (app1 = UnderReview on job1)
SEEDED_APPS=$(get "/api/applications?jobId=$JOB_ID" "$HR_TOKEN" || echo "[]")
SEEDED_APP_ID=$(echo "$SEEDED_APPS" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'])" 2>/dev/null || echo "")

if [[ -n "$SEEDED_APP_ID" ]]; then
  info "PATCH /api/applications/{id}/status → Shortlisted"
  check_http "PATCH application status → Shortlisted" "/api/applications/$SEEDED_APP_ID/status" "PATCH" "$HR_TOKEN" \
    '{"newStatus":"Shortlisted"}'

  info "PATCH /api/applications/{id}/status → Accepted"
  check_http "PATCH application status → Accepted" "/api/applications/$SEEDED_APP_ID/status" "PATCH" "$HR_TOKEN" \
    '{"newStatus":"Accepted"}'
else
  info "No seeded applications found under job1 — skipping HR review steps"
fi

# ─────────────────────────────────────────────────────────────────────────────
section "7 · COMPANIES"
# ─────────────────────────────────────────────────────────────────────────────

info "POST /api/companies (HR creates company)"
COMP_RESP=$(post "/api/companies" \
  '{"name":"Innovate Labs","description":"A product startup building the next generation of HR tools using AI.","website":"https://innovatelabs.example.com"}' \
  "$HR_TOKEN" || echo "{}")
NEW_COMP_ID=$(echo "$COMP_RESP" | json "['id']" 2>/dev/null || echo "")
[[ -n "$NEW_COMP_ID" ]] && pass "POST /api/companies → id: $NEW_COMP_ID" || fail "POST /api/companies"

info "GET /api/companies/mine"
MY_COMPS=$(get "/api/companies/mine" "$HR_TOKEN")
COMP_COUNT=$(echo "$MY_COMPS" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
[[ "$COMP_COUNT" -ge 1 ]] && pass "GET /api/companies/mine → $COMP_COUNT company(ies)" || fail "GET /api/companies/mine"

# ─────────────────────────────────────────────────────────────────────────────
section "8 · NOTIFICATIONS"
# ─────────────────────────────────────────────────────────────────────────────

info "GET /api/notifications (candidate)"
NOTIFS=$(get "/api/notifications" "$CAND_TOKEN")
NOTIF_COUNT=$(echo "$NOTIFS" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
pass "GET /api/notifications → $NOTIF_COUNT notification(s)"

NOTIF_ID=$(echo "$NOTIFS" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d[0]['id'])" 2>/dev/null || echo "")
if [[ -n "$NOTIF_ID" ]]; then
  info "PATCH /api/notifications/{id}/read"
  check_http "PATCH /api/notifications/$NOTIF_ID/read" "/api/notifications/$NOTIF_ID/read" "PATCH" "$CAND_TOKEN" "{}"

  info "PATCH /api/notifications/read-all"
  check_http "PATCH /api/notifications/read-all" "/api/notifications/read-all" "PATCH" "$CAND_TOKEN" "{}"
else
  info "No notifications to mark read — skipping"
fi

# ─────────────────────────────────────────────────────────────────────────────
section "9 · ADMIN"
# ─────────────────────────────────────────────────────────────────────────────

info "GET /api/admin/users"
USERS=$(get "/api/admin/users" "$ADMIN_TOKEN")
USER_COUNT=$(echo "$USERS" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
[[ "$USER_COUNT" -ge 3 ]] && pass "GET /api/admin/users → $USER_COUNT users" || fail "GET /api/admin/users (got $USER_COUNT)"

info "GET /api/admin/jobs"
ALL_JOBS=$(get "/api/admin/jobs" "$ADMIN_TOKEN")
ALL_JOBS_COUNT=$(echo "$ALL_JOBS" | python3 -c "import sys,json; print(len(json.load(sys.stdin)))" 2>/dev/null || echo "0")
[[ "$ALL_JOBS_COUNT" -ge 3 ]] && pass "GET /api/admin/jobs → $ALL_JOBS_COUNT jobs" || fail "GET /api/admin/jobs (got $ALL_JOBS_COUNT)"

# Deactivate the newly registered test user (if registration succeeded)
if [[ -n "$REG_ID" ]]; then
  info "PATCH /api/admin/users/{id}/deactivate"
  check_http "PATCH /api/admin/users/$REG_ID/deactivate" "/api/admin/users/$REG_ID/deactivate" "PATCH" "$ADMIN_TOKEN" "{}"
fi

# ─────────────────────────────────────────────────────────────────────────────
section "10 · SECURITY — Auth Guards"
# ─────────────────────────────────────────────────────────────────────────────

info "GET /api/identity/me without token → 401"
STATUS=$(curl -so /dev/null -w "%{http_code}" "$BASE/api/identity/me")
[[ "$STATUS" == "401" ]] && pass "Unauthenticated /me → 401" || fail "Unauthenticated /me → expected 401, got $STATUS"

info "GET /api/admin/users as Candidate → 403"
STATUS=$(curl -so /dev/null -w "%{http_code}" "$BASE/api/admin/users" -H "Authorization: Bearer $CAND_TOKEN")
[[ "$STATUS" == "403" ]] && pass "Candidate on admin endpoint → 403" || fail "Candidate on admin endpoint → expected 403, got $STATUS"

info "POST /api/identity/login with wrong password → 400/401"
STATUS=$(curl -so /dev/null -w "%{http_code}" -X POST "$BASE/api/identity/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@talentbridge.com","password":"wrongpassword"}')
[[ "$STATUS" == "400" || "$STATUS" == "401" ]] && pass "Wrong password → $STATUS" || fail "Wrong password → expected 400/401, got $STATUS"

# ─────────────────────────────────────────────────────────────────────────────
section "RESULTS"
# ─────────────────────────────────────────────────────────────────────────────
TOTAL=$((PASS + FAIL))
echo ""
echo -e "  Tests run:   ${TOTAL}"
echo -e "  ${GREEN}Passed:${NC}      ${PASS}"
[[ $FAIL -gt 0 ]] && echo -e "  ${RED}Failed:${NC}      ${FAIL}" || echo -e "  Failed:      0"
echo ""
if [[ $FAIL -eq 0 ]]; then
  echo -e "${GREEN}  ✓ All $TOTAL checks passed — API is healthy!${NC}"
else
  echo -e "${RED}  ✗ $FAIL check(s) failed. Review output above.${NC}"
  exit 1
fi
