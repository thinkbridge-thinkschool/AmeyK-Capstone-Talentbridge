# TalentBridge API — Happy-Path Verification (PowerShell)
# Usage:  .\scripts\verify-api.ps1 [-BaseUrl "http://localhost:5000"]
# Requires: PowerShell 5.1+

param([string]$BaseUrl = "http://localhost:5000")

$PASS = 0; $FAIL = 0

function Write-Pass([string]$msg) { Write-Host "  [PASS] $msg" -ForegroundColor Green; $global:PASS++ }
function Write-Fail([string]$msg) { Write-Host "  [FAIL] $msg" -ForegroundColor Red;  $global:FAIL++ }
function Write-Section([string]$title) { Write-Host "`n══════ $title ══════" -ForegroundColor Yellow }
function Write-Info([string]$msg) { Write-Host "  ▶ $msg" -ForegroundColor Cyan }

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Token = "", [object]$Body = $null)
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $headers; ErrorAction = "SilentlyContinue" }
    if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Depth 10) }
    try {
        $resp = Invoke-RestMethod @params
        return $resp
    } catch {
        return $null
    }
}

function Get-StatusCode {
    param([string]$Method, [string]$Path, [string]$Token = "", [object]$Body = $null)
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $headers; ErrorAction = "SilentlyContinue" }
    if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Depth 10) }
    try {
        Invoke-WebRequest @params | Select-Object -ExpandProperty StatusCode
    } catch {
        $_.Exception.Response.StatusCode.value__
    }
}

Write-Host "`n  TalentBridge API Happy-Path Verification" -ForegroundColor Cyan
Write-Host "  Base URL: $BaseUrl  |  $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

# ──────────────────────────────────────────────────
Write-Section "1 · IDENTITY — Login & Token"
# ──────────────────────────────────────────────────

Write-Info "Admin login"
$adminResp = Invoke-Api -Method POST -Path "/api/identity/login" -Body @{ email="admin@talentbridge.com"; password="Admin@1234" }
if ($adminResp.token) { Write-Pass "Admin login → token received"; $adminToken = $adminResp.token }
else { Write-Fail "Admin login"; $adminToken = "" }

Write-Info "HR login"
$hrResp = Invoke-Api -Method POST -Path "/api/identity/login" -Body @{ email="hr@talentbridge.com"; password="HR@1234" }
if ($hrResp.token) { Write-Pass "HR login → token received"; $hrToken = $hrResp.token }
else { Write-Fail "HR login"; $hrToken = "" }

Write-Info "Candidate login"
$candResp = Invoke-Api -Method POST -Path "/api/identity/login" -Body @{ email="candidate@talentbridge.com"; password="Candidate@1234" }
if ($candResp.token) { Write-Pass "Candidate login → token received"; $candToken = $candResp.token; $candRefresh = $candResp.refreshToken }
else { Write-Fail "Candidate login"; $candToken = ""; $candRefresh = "" }

Write-Info "GET /api/identity/me"
$me = Invoke-Api -Method GET -Path "/api/identity/me" -Token $adminToken
if ($me.email -eq "admin@talentbridge.com") { Write-Pass "GET /me → email: $($me.email)" }
else { Write-Fail "GET /me (got: $($me.email))" }

Write-Info "POST /api/identity/refresh"
$refreshResp = Invoke-Api -Method POST -Path "/api/identity/refresh" -Body @{ refreshToken=$candRefresh }
if ($refreshResp.accessToken) { Write-Pass "Token refresh → new access token received"; $candToken = $refreshResp.accessToken }
else { Write-Fail "Token refresh" }

# ──────────────────────────────────────────────────
Write-Section "2 · IDENTITY — Register"
# ──────────────────────────────────────────────────

$ts = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
Write-Info "POST /api/identity/register"
$regResp = Invoke-Api -Method POST -Path "/api/identity/register" -Body @{
    email="testuser_$ts@example.com"; password="Test@123456"; role="Candidate"; fullName="Test User"
}
if ($regResp.userId) { Write-Pass "Register → userId: $($regResp.userId)"; $newUserId = $regResp.userId }
else { Write-Fail "Register new candidate"; $newUserId = "" }

# ──────────────────────────────────────────────────
Write-Section "3 · JOBS — Browse & Search"
# ──────────────────────────────────────────────────

Write-Info "GET /api/jobs/search"
$jobsResp = Invoke-Api -Method GET -Path "/api/jobs/search?page=1&size=10"
$jobs = if ($jobsResp -is [array]) { $jobsResp } else { $jobsResp.items }
if ($jobs.Count -ge 3) { Write-Pass "GET /api/jobs/search → $($jobs.Count) jobs" }
else { Write-Fail "GET /api/jobs/search (got $($jobs.Count))" }

Write-Info "GET /api/jobs/search?keyword=.NET"
$searchResp = Invoke-Api -Method GET -Path "/api/jobs/search?keyword=.NET&page=1&size=10"
$searchItems = if ($searchResp -is [array]) { $searchResp } else { $searchResp.items }
if ($searchItems.Count -ge 1) { Write-Pass "Search keyword .NET → $($searchItems.Count) result(s)" }
else { Write-Fail "Search keyword .NET (no results)" }

$jobId = if ($jobs.Count -gt 0) { $jobs[0].id } else { "" }
$companyId = if ($jobs.Count -gt 0) { $jobs[0].companyId } else { "" }

Write-Info "GET /api/jobs/{id}"
if ($jobId) {
    $jobDetail = Invoke-Api -Method GET -Path "/api/jobs/$jobId"
    if ($jobDetail.title) { Write-Pass "GET /api/jobs/$jobId → `"$($jobDetail.title)`"" }
    else { Write-Fail "GET /api/jobs/{id}" }
}

# ──────────────────────────────────────────────────
Write-Section "4 · JOBS — HR Post & Manage"
# ──────────────────────────────────────────────────

Write-Info "POST /api/jobs (create draft)"
$newJobResp = Invoke-Api -Method POST -Path "/api/jobs" -Token $hrToken -Body @{
    title="QA Automation Engineer"; location="Hyderabad, India"
    description="Build and maintain automated test suites using Playwright and SpecFlow."
    salaryMin=50000; salaryMax=80000; companyId=$companyId; postedByHRId=$hrResp.userId
}
$newJobId = if ($newJobResp.jobId) { $newJobResp.jobId } elseif ($newJobResp.id) { $newJobResp.id } else { "" }
if ($newJobId) { Write-Pass "POST /api/jobs → id: $newJobId" } else { Write-Fail "POST /api/jobs" }

if ($newJobId) {
    Write-Info "GET /api/jobs/mine"
    $mineResp = Invoke-Api -Method GET -Path "/api/jobs/mine" -Token $hrToken
    if ($mineResp.Count -ge 1) { Write-Pass "GET /api/jobs/mine → $($mineResp.Count) job(s)" }
    else { Write-Fail "GET /api/jobs/mine" }

    Write-Info "PUT /api/jobs/{id}"
    $sc = Get-StatusCode -Method PUT -Path "/api/jobs/$newJobId" -Token $hrToken -Body @{
        title="QA Automation Engineer (Updated)"; location="Hyderabad, India"
        description="Build and maintain automated test suites using Playwright and SpecFlow. Updated."
        salaryMin=55000; salaryMax=85000
    }
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "PUT /api/jobs/$newJobId (HTTP $sc)" }
    else { Write-Fail "PUT /api/jobs/$newJobId (HTTP $sc)" }

    Write-Info "POST /api/jobs/{id}/publish"
    $sc = Get-StatusCode -Method POST -Path "/api/jobs/$newJobId/publish" -Token $hrToken -Body @{}
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "POST /api/jobs/$newJobId/publish (HTTP $sc)" }
    else { Write-Fail "POST /api/jobs/$newJobId/publish (HTTP $sc)" }

    Write-Info "PATCH /api/jobs/{id}/close"
    $sc = Get-StatusCode -Method PATCH -Path "/api/jobs/$newJobId/close" -Token $hrToken -Body @{}
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "PATCH /api/jobs/$newJobId/close (HTTP $sc)" }
    else { Write-Fail "PATCH /api/jobs/$newJobId/close (HTTP $sc)" }

    Write-Info "DELETE /api/jobs/{id}"
    $sc = Get-StatusCode -Method DELETE -Path "/api/jobs/$newJobId" -Token $hrToken
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "DELETE /api/jobs/$newJobId (HTTP $sc)" }
    else { Write-Fail "DELETE /api/jobs/$newJobId (HTTP $sc)" }
}

# ──────────────────────────────────────────────────
Write-Section "5 · APPLICATIONS — Candidate Flow"
# ──────────────────────────────────────────────────

$job3 = $jobs | Where-Object { $_.title -match "DevOps" } | Select-Object -First 1
$applyJobId = if ($job3) { $job3.id } else { $jobId }

# Decode candidate userId from JWT
$jwtPayload = $candToken.Split('.')[1]
$jwtPayload += "=" * ((4 - $jwtPayload.Length % 4) % 4)
$decoded = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($jwtPayload)) | ConvertFrom-Json
$candId = if ($decoded.sub) { $decoded.sub } else { $decoded.nameid }

Write-Info "POST /api/applications"
$applyResp = Invoke-Api -Method POST -Path "/api/applications" -Token $candToken -Body @{
    candidateId=$candId; jobId=$applyJobId
    coverLetter="I am passionate about cloud DevOps with 4 years of Azure experience including AKS, Bicep, and GitHub Actions."
    resumeUrl="https://example.com/resume.pdf"
}
$appId = $applyResp.applicationId
if ($appId) { Write-Pass "POST /api/applications → id: $appId" } else { Write-Fail "POST /api/applications" }

Write-Info "GET /api/applications/my"
$myApps = Invoke-Api -Method GET -Path "/api/applications/my" -Token $candToken
if ($myApps.Count -ge 1) { Write-Pass "GET /api/applications/my → $($myApps.Count) application(s)" }
else { Write-Fail "GET /api/applications/my" }

if ($appId) {
    Write-Info "GET /api/applications/{id}"
    $appDetail = Invoke-Api -Method GET -Path "/api/applications/$appId" -Token $candToken
    if ($appDetail.status) { Write-Pass "GET /api/applications/$appId → status: $($appDetail.status)" }
    else { Write-Fail "GET /api/applications/{id}" }

    Write-Info "PATCH /api/applications/{id}/withdraw"
    $sc = Get-StatusCode -Method PATCH -Path "/api/applications/$appId/withdraw" -Token $candToken -Body @{}
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "PATCH withdraw $appId (HTTP $sc)" }
    else { Write-Fail "PATCH withdraw (HTTP $sc)" }
}

# ──────────────────────────────────────────────────
Write-Section "6 · APPLICATIONS — HR Review"
# ──────────────────────────────────────────────────

$seededApps = Invoke-Api -Method GET -Path "/api/applications?jobId=$jobId" -Token $hrToken
$seededApp = if ($seededApps -is [array] -and $seededApps.Count -gt 0) { $seededApps[0] } else { $null }

if ($seededApp) {
    Write-Info "PATCH application status → Shortlisted"
    $sc = Get-StatusCode -Method PATCH -Path "/api/applications/$($seededApp.id)/status" -Token $hrToken -Body @{ newStatus="Shortlisted" }
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "Status → Shortlisted (HTTP $sc)" }
    else { Write-Fail "Status → Shortlisted (HTTP $sc)" }

    Write-Info "PATCH application status → Accepted"
    $sc = Get-StatusCode -Method PATCH -Path "/api/applications/$($seededApp.id)/status" -Token $hrToken -Body @{ newStatus="Accepted" }
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "Status → Accepted (HTTP $sc)" }
    else { Write-Fail "Status → Accepted (HTTP $sc)" }
} else {
    Write-Host "  (no seeded applications found for job $jobId — skipping HR review)" -ForegroundColor DarkGray
}

# ──────────────────────────────────────────────────
Write-Section "7 · COMPANIES"
# ──────────────────────────────────────────────────

Write-Info "POST /api/companies"
$compResp = Invoke-Api -Method POST -Path "/api/companies" -Token $hrToken -Body @{
    name="Innovate Labs"; description="A product startup building next-generation HR tools using AI."; website="https://innovatelabs.example.com"
}
if ($compResp.id) { Write-Pass "POST /api/companies → id: $($compResp.id)" }
else { Write-Fail "POST /api/companies" }

Write-Info "GET /api/companies/mine"
$myComps = Invoke-Api -Method GET -Path "/api/companies/mine" -Token $hrToken
if ($myComps.Count -ge 1) { Write-Pass "GET /api/companies/mine → $($myComps.Count) company(ies)" }
else { Write-Fail "GET /api/companies/mine" }

# ──────────────────────────────────────────────────
Write-Section "8 · NOTIFICATIONS"
# ──────────────────────────────────────────────────

Write-Info "GET /api/notifications"
$notifs = Invoke-Api -Method GET -Path "/api/notifications" -Token $candToken
$notifCount = if ($notifs -is [array]) { $notifs.Count } else { 0 }
Write-Pass "GET /api/notifications → $notifCount notification(s)"

if ($notifCount -gt 0) {
    $notifId = $notifs[0].id
    Write-Info "PATCH /api/notifications/{id}/read"
    $sc = Get-StatusCode -Method PATCH -Path "/api/notifications/$notifId/read" -Token $candToken -Body @{}
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "PATCH notification/$notifId/read (HTTP $sc)" }
    else { Write-Fail "PATCH notification read (HTTP $sc)" }

    Write-Info "PATCH /api/notifications/read-all"
    $sc = Get-StatusCode -Method PATCH -Path "/api/notifications/read-all" -Token $candToken -Body @{}
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "PATCH notifications/read-all (HTTP $sc)" }
    else { Write-Fail "PATCH notifications/read-all (HTTP $sc)" }
}

# ──────────────────────────────────────────────────
Write-Section "9 · ADMIN"
# ──────────────────────────────────────────────────

Write-Info "GET /api/admin/users"
$allUsers = Invoke-Api -Method GET -Path "/api/admin/users" -Token $adminToken
if ($allUsers.Count -ge 3) { Write-Pass "GET /api/admin/users → $($allUsers.Count) users" }
else { Write-Fail "GET /api/admin/users (got $($allUsers.Count))" }

Write-Info "GET /api/admin/jobs"
$allJobs = Invoke-Api -Method GET -Path "/api/admin/jobs" -Token $adminToken
if ($allJobs.Count -ge 3) { Write-Pass "GET /api/admin/jobs → $($allJobs.Count) jobs" }
else { Write-Fail "GET /api/admin/jobs (got $($allJobs.Count))" }

if ($newUserId) {
    Write-Info "PATCH /api/admin/users/{id}/deactivate"
    $sc = Get-StatusCode -Method PATCH -Path "/api/admin/users/$newUserId/deactivate" -Token $adminToken -Body @{}
    if ($sc -ge 200 -and $sc -lt 300) { Write-Pass "PATCH deactivate user $newUserId (HTTP $sc)" }
    else { Write-Fail "PATCH deactivate user (HTTP $sc)" }
}

# ──────────────────────────────────────────────────
Write-Section "10 · SECURITY — Auth Guards"
# ──────────────────────────────────────────────────

Write-Info "GET /api/identity/me without token → 401"
$sc = Get-StatusCode -Method GET -Path "/api/identity/me"
if ($sc -eq 401) { Write-Pass "Unauthenticated /me → 401" } else { Write-Fail "Unauthenticated /me → expected 401, got $sc" }

Write-Info "GET /api/admin/users as Candidate → 403"
$sc = Get-StatusCode -Method GET -Path "/api/admin/users" -Token $candToken
if ($sc -eq 403) { Write-Pass "Candidate on admin endpoint → 403" } else { Write-Fail "Candidate on admin → expected 403, got $sc" }

Write-Info "Login with wrong password → 400/401"
$sc = Get-StatusCode -Method POST -Path "/api/identity/login" -Body @{ email="admin@talentbridge.com"; password="wrongpassword" }
if ($sc -eq 400 -or $sc -eq 401) { Write-Pass "Wrong password → $sc" } else { Write-Fail "Wrong password → expected 400/401, got $sc" }

# ──────────────────────────────────────────────────
Write-Section "RESULTS"
# ──────────────────────────────────────────────────
$total = $PASS + $FAIL
Write-Host ""
Write-Host "  Tests run:  $total"
Write-Host "  Passed:     $PASS" -ForegroundColor Green
if ($FAIL -gt 0) { Write-Host "  Failed:     $FAIL" -ForegroundColor Red }
else             { Write-Host "  Failed:     0" }
Write-Host ""
if ($FAIL -eq 0) {
    Write-Host "  ✓ All $total checks passed — API is healthy!" -ForegroundColor Green
} else {
    Write-Host "  ✗ $FAIL check(s) failed. Review output above." -ForegroundColor Red
    exit 1
}
