# TalentBridge Seed Data Script
# Creates 50 records: 5 HR users, 10 candidates, 20 jobs (published), 15 applications
# Usage: .\scripts\seed-data.ps1 [-BaseUrl "http://localhost:5237"]

param(
    [string]$BaseUrl = "http://localhost:5237"
)

$ErrorActionPreference = "Stop"

function Write-Step { param([string]$msg) Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$msg) Write-Host "  OK: $msg"    -ForegroundColor Green }
function Write-Warn { param([string]$msg) Write-Host "  WARN: $msg"  -ForegroundColor Yellow }

# ── 1. Register HR users ──────────────────────────────────────────────────────
Write-Step "Registering 5 HR users (role=1)"

$hrUsers = @(
    @{ email = "hr.alice@techcorp.io";   password = "HrPass1!" }
    @{ email = "hr.bob@futuresoft.io";   password = "HrPass2!" }
    @{ email = "hr.carol@datavault.io";  password = "HrPass3!" }
    @{ email = "hr.david@cloudnine.io";  password = "HrPass4!" }
    @{ email = "hr.eva@nexuslabs.io";    password = "HrPass5!" }
)

foreach ($u in $hrUsers) {
    try {
        $body = @{ email = $u.email; password = $u.password; role = 1 } | ConvertTo-Json
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/register" `
            -ContentType "application/json" -Body $body | Out-Null
        Write-Ok "Registered $($u.email)"
    } catch {
        Write-Warn "Could not register $($u.email) — may already exist"
    }
}

# ── 2. Register Candidate users ───────────────────────────────────────────────
Write-Step "Registering 10 Candidate users (role=0)"

$candidates = @(
    @{ email = "john.smith@gmail.com";    password = "CandPass1!" }
    @{ email = "jane.doe@outlook.com";    password = "CandPass2!" }
    @{ email = "mike.chen@gmail.com";     password = "CandPass3!" }
    @{ email = "sara.ali@yahoo.com";      password = "CandPass4!" }
    @{ email = "tom.brown@gmail.com";     password = "CandPass5!" }
    @{ email = "amy.wang@outlook.com";    password = "CandPass6!" }
    @{ email = "raj.patel@gmail.com";     password = "CandPass7!" }
    @{ email = "lisa.jones@gmail.com";    password = "CandPass8!" }
    @{ email = "kevin.oh@yahoo.com";      password = "CandPass9!" }
    @{ email = "nina.ross@outlook.com";   password = "CandPass0!" }
)

foreach ($u in $candidates) {
    try {
        $body = @{ email = $u.email; password = $u.password; role = 0 } | ConvertTo-Json
        Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/register" `
            -ContentType "application/json" -Body $body | Out-Null
        Write-Ok "Registered $($u.email)"
    } catch {
        Write-Warn "Could not register $($u.email) — may already exist"
    }
}

# ── 3. Login HR users and get tokens ─────────────────────────────────────────
Write-Step "Logging in HR users"

$hrTokens = @()
foreach ($u in $hrUsers) {
    try {
        $body = @{ email = $u.email; password = $u.password } | ConvertTo-Json
        $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" `
            -ContentType "application/json" -Body $body
        $hrTokens += $resp.token
        Write-Ok "Logged in $($u.email)"
    } catch {
        Write-Warn "Login failed for $($u.email)"
        $hrTokens += $null
    }
}

# ── 4. Post 20 jobs (4 per HR user) ──────────────────────────────────────────
Write-Step "Posting 20 jobs (4 per HR user)"

$jobTemplates = @(
    @{ title = "Senior Full Stack Developer";      desc = "Build scalable web applications using .NET and Angular. You will design and implement end-to-end features, collaborate with product teams, and lead code reviews. Required: 5+ years .NET, TypeScript, SQL Server.";       location = "New York, NY";     min = 90000; max = 130000 }
    @{ title = "Cloud Infrastructure Engineer";    desc = "Design and maintain Azure cloud infrastructure. Experience with Bicep/ARM templates, AKS, App Service, and monitoring (Application Insights). Azure certifications preferred.";   location = "Remote";           min = 95000; max = 140000 }
    @{ title = "Data Science Lead";                desc = "Lead a team of data scientists building ML models for hiring predictions. Proficiency in Python, scikit-learn, Azure ML. 7+ years of data science experience required.";             location = "San Francisco, CA"; min = 120000; max = 170000 }
    @{ title = "DevOps Engineer";                  desc = "Own CI/CD pipelines, container orchestration, and infrastructure automation. Kubernetes, Docker, GitHub Actions, Terraform. On-call rotation required.";                           location = "Austin, TX";       min = 85000; max = 120000 }
    @{ title = "Product Manager — Platform";       desc = "Drive the roadmap for our enterprise SaaS hiring platform. Work closely with engineering, design, and customers to define and deliver features. 5+ years PM experience.";            location = "Seattle, WA";      min = 100000; max = 145000 }
    @{ title = "Frontend Engineer (Angular)";      desc = "Build beautiful, performant UI components with Angular 19 and Tailwind CSS. Collaborate with UX designers to implement pixel-perfect interfaces. TypeScript expert.";              location = "Remote";           min = 80000; max = 115000 }
    @{ title = "Backend API Engineer (.NET)";      desc = "Design RESTful APIs using ASP.NET Core 10 following clean architecture principles. Build and maintain modular services. Experience with EF Core and Azure SQL required.";          location = "Chicago, IL";      min = 85000; max = 125000 }
    @{ title = "UX Designer";                      desc = "Design intuitive hiring workflows for HR teams and candidates. Proficiency in Figma, strong portfolio of B2B SaaS products. Collaborate with product and engineering.";             location = "New York, NY";     min = 75000; max = 110000 }
    @{ title = "Technical Recruiter";              desc = "Source and screen top engineering talent for fast-growing tech companies. Build pipelines, conduct phone screens, and coordinate interview loops. 3+ years in tech recruiting.";   location = "Boston, MA";       min = 65000; max = 90000 }
    @{ title = "QA Automation Engineer";           desc = "Build automated test suites using Playwright and xUnit. Own test strategy for APIs and UI. Experience with CI integration and performance testing a plus.";                       location = "Remote";           min = 75000; max = 105000 }
    @{ title = "Security Engineer";                desc = "Protect our platform from emerging threats. Conduct security reviews, pen testing, and implement zero-trust controls on Azure. CISSP or equivalent certification required.";       location = "Washington, DC";   min = 105000; max = 150000 }
    @{ title = "Machine Learning Engineer";        desc = "Train and deploy ML models for resume screening and job matching. Python, PyTorch, Azure ML, MLflow. Experience productionising models at scale required.";                       location = "San Francisco, CA"; min = 110000; max = 160000 }
    @{ title = "Database Administrator";           desc = "Manage Azure SQL and Cosmos DB instances supporting millions of records. Query optimisation, backup/recovery, and HA configuration. Azure DBA certification preferred.";          location = "Dallas, TX";       min = 80000; max = 115000 }
    @{ title = "Technical Writer";                 desc = "Create developer documentation, API references, and onboarding guides. Familiarity with OpenAPI/Swagger and markdown-based docs systems. Prior experience in SaaS preferred.";    location = "Remote";           min = 65000; max = 90000 }
    @{ title = "Scrum Master / Agile Coach";       desc = "Facilitate sprint ceremonies, remove blockers, and coach 3 cross-functional teams on Agile best practices. SAFe certification a strong advantage.";                              location = "Atlanta, GA";      min = 85000; max = 115000 }
    @{ title = "Mobile Developer (iOS)";           desc = "Build native iOS apps in Swift for candidates on the go. Experience with REST API integration, push notifications, and App Store deployment. 4+ years Swift required.";          location = "Los Angeles, CA";  min = 95000; max = 135000 }
    @{ title = "Solutions Architect";              desc = "Design end-to-end enterprise integration architectures. Azure, microservices, event-driven patterns, and API gateway expertise. Customer-facing pre-sales experience a plus.";   location = "Remote";           min = 130000; max = 180000 }
    @{ title = "HR Operations Specialist";         desc = "Manage onboarding, compliance, and HR data for a 200-person remote team. Proficiency in HRIS systems, benefits administration, and employment law fundamentals.";               location = "New York, NY";     min = 55000; max = 75000 }
    @{ title = "Customer Success Manager";         desc = "Own the success of 30+ enterprise accounts on TalentBridge. Conduct QBRs, drive adoption, and reduce churn. Experience with hiring software or HCM systems preferred.";         location = "Chicago, IL";      min = 75000; max = 100000 }
    @{ title = "Site Reliability Engineer";        desc = "Maintain 99.99% uptime for our global hiring platform. On-call, incident response, observability with Application Insights and Grafana. Kubernetes and Go expertise valued.";   location = "Remote";           min = 110000; max = 155000 }
)

$companyId = "00000000-0000-0000-0000-000000000001"
$postedJobIds = @()

for ($i = 0; $i -lt $jobTemplates.Count; $i++) {
    $hrIdx = $i % $hrTokens.Count
    $token = $hrTokens[$hrIdx]
    if (-not $token) { Write-Warn "No token for HR $hrIdx, skipping job $i"; continue }

    $jt = $jobTemplates[$i]
    try {
        $body = @{
            title        = $jt.title
            description  = $jt.desc
            location     = $jt.location
            salaryMin    = $jt.min
            salaryMax    = $jt.max
            companyId    = $companyId
            postedByHRId = "00000000-0000-0000-0000-000000000099"
        } | ConvertTo-Json
        $headers = @{ Authorization = "Bearer $token" }
        $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/jobs" `
            -ContentType "application/json" -Headers $headers -Body $body
        $postedJobIds += $resp.id
        Write-Ok "Posted job: $($jt.title) [ID: $($resp.id)]"
    } catch {
        Write-Warn "Failed to post job '$($jt.title)': $($_.Exception.Message)"
    }
}

# ── 5. Publish all jobs ───────────────────────────────────────────────────────
Write-Step "Publishing $($postedJobIds.Count) jobs"

for ($i = 0; $i -lt $postedJobIds.Count; $i++) {
    $jobId = $postedJobIds[$i]
    $hrIdx = $i % $hrTokens.Count
    $token = $hrTokens[$hrIdx]
    if (-not $token) { continue }

    try {
        $headers = @{ Authorization = "Bearer $token" }
        Invoke-RestMethod -Method Patch -Uri "$BaseUrl/api/jobs/$jobId/publish" `
            -ContentType "application/json" -Headers $headers | Out-Null
        Write-Ok "Published job $jobId"
    } catch {
        Write-Warn "Could not publish job $jobId — $($_.Exception.Message)"
    }
}

# ── 6. Login candidates ───────────────────────────────────────────────────────
Write-Step "Logging in candidates"

$candidateTokens = @()
foreach ($u in $candidates) {
    try {
        $body = @{ email = $u.email; password = $u.password } | ConvertTo-Json
        $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" `
            -ContentType "application/json" -Body $body
        $candidateTokens += $resp.token
        Write-Ok "Logged in $($u.email)"
    } catch {
        Write-Warn "Login failed for $($u.email)"
        $candidateTokens += $null
    }
}

# ── 7. Submit 15 applications ─────────────────────────────────────────────────
Write-Step "Submitting 15 applications"

$coverLetters = @(
    "I am excited to apply for this role. With 6 years of experience in full-stack development using .NET and Angular, I have built high-traffic platforms handling millions of requests. I thrive in collaborative environments and am passionate about clean, maintainable code."
    "This opportunity aligns perfectly with my background. Over the past 5 years I have designed and deployed Azure infrastructure for multiple SaaS products. I hold the Azure Solutions Architect Expert certification and enjoy mentoring junior engineers."
    "Having led data science initiatives at two fintech startups, I bring a strong combination of ML expertise and business intuition. I am particularly excited about applying predictive models to improve hiring outcomes at scale."
    "As a DevOps engineer with 4 years automating CI/CD pipelines and managing Kubernetes clusters, I have reduced deployment times by 70% at my current company. I am looking for my next challenge in a product-focused environment."
    "Your product excites me — improving the hiring process for both candidates and HR teams is a goal I am passionate about. My 7 years as a product manager in enterprise SaaS, including two successful platform launches, make me a strong fit."
    "I have been building Angular applications for 5 years and recently migrated a large codebase from AngularJS to Angular 18. I love the challenge of creating accessible, performant UIs and would love to contribute to your frontend."
    "With deep expertise in ASP.NET Core and clean architecture, I have delivered robust APIs serving 10 million daily requests. I follow DDD principles and have experience with both monolithic and microservice architectures."
    "As a UX designer specialising in B2B SaaS, I have redesigned onboarding flows that improved activation rates by 40%. I work closely with engineers and believe great design is inseparable from great engineering."
    "I am a technical recruiter with a track record of placing 50+ engineers per year in competitive markets. I have experience using data-driven sourcing strategies and ATS platforms. I would love to use TalentBridge from the inside."
    "Quality is my passion. I have built Playwright test suites from scratch covering 90% of critical user journeys, integrated into GitHub Actions CI. I catch bugs before users do — and I love doing it."
    "Security is not an afterthought for me — it is the foundation. I have conducted penetration tests, implemented OWASP Top 10 mitigations, and designed zero-trust architectures on Azure. I hold CISSP and AZ-500 certifications."
    "My ML models for resume screening have been deployed at two Fortune 500 companies, reducing time-to-shortlist by 60%. I am passionate about responsible AI and ensuring fairness in automated hiring decisions."
    "I have administered Azure SQL databases with 500GB+ of data, optimised slow queries cutting P99 latency from 2s to 80ms, and set up geo-redundant DR configurations. I am looking for a team where performance matters."
    "I love turning complex technical concepts into clear, approachable documentation. My API guides and developer tutorials have been praised by thousands of developers integrating our platform. Docs are code, and I treat them that way."
    "I have been a Scrum Master for 4 years across distributed teams in 3 time zones. I am SAFe certified and have helped teams improve velocity by 35% through better sprint hygiene and retrospective practices."
)

$appliedCount = 0
for ($i = 0; $i -lt 15; $i++) {
    if ($postedJobIds.Count -eq 0) { Write-Warn "No job IDs available"; break }
    $jobId      = $postedJobIds[$i % $postedJobIds.Count]
    $candIdx    = $i % $candidateTokens.Count
    $token      = $candidateTokens[$candIdx]
    if (-not $token) { Write-Warn "No token for candidate $candIdx"; continue }

    try {
        $body = @{
            candidateId  = "00000000-0000-0000-0000-$("$candIdx".PadLeft(12, '0'))"
            jobId        = $jobId
            coverLetter  = $coverLetters[$i]
            resumeUrl    = "https://drive.google.com/resumes/candidate-$($candIdx + 1).pdf"
        } | ConvertTo-Json
        $headers = @{ Authorization = "Bearer $token" }
        $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/applications" `
            -ContentType "application/json" -Headers $headers -Body $body
        $appliedCount++
        Write-Ok "Application submitted [ID: $($resp.id)] for job $jobId"
    } catch {
        Write-Warn "Application failed for job $jobId — $($_.Exception.Message)"
    }
}

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host "`n" + ("=" * 60) -ForegroundColor White
Write-Host "SEED COMPLETE" -ForegroundColor White
Write-Host ("=" * 60) -ForegroundColor White
Write-Host "  HR users registered   : $($hrUsers.Count)" -ForegroundColor White
Write-Host "  Candidates registered : $($candidates.Count)" -ForegroundColor White
Write-Host "  Jobs posted           : $($postedJobIds.Count)" -ForegroundColor White
Write-Host "  Applications made     : $appliedCount" -ForegroundColor White
Write-Host "  TOTAL records         : $($hrUsers.Count + $candidates.Count + $postedJobIds.Count + $appliedCount)" -ForegroundColor White
Write-Host ""
Write-Host "Browse jobs at: http://localhost:4200/jobs" -ForegroundColor Cyan
