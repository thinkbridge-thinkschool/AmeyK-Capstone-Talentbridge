# Day 29 — Build Day 1: Foundation + Happy Path

## Links

- **Repo:** https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge
- **Branch:** `feature/angular-frontend`
- **PR #8:** https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge/pull/8
- **CI Run:** https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge/actions/runs/28007817642
- **Live App:** https://talentbridge-api-amey.azurewebsites.net
- **Swagger:** https://talentbridge-api-amey.azurewebsites.net/swagger

---

## Commit Log (2026-06-23)

```
201f761  fix(register): send role as string name not numeric value
5f987d9  feat(deploy): serve Angular from App Service wwwroot (drop SWA)
30c3f66  fix(deploy): try swa@1.1.10 (latest 1.x with different binary hash)
569007a  fix(deploy): pin swa CLI to v1.9.7 — v2.0.9 binary hash not on CDN
7a3f8c7  debug(deploy): add ldd diagnostic + install missing native libs for SWA
4420eef  fix(deploy): use ubuntu-22.04 for SWA — StaticSitesClient needs libssl1.1
6bfc2c7  fix(deploy): replace SWA GitHub Action with swa CLI deploy
f4cdd9d  fix(deploy): switch backend to zip deploy, fix SWA skip_app_build
19c2ef1  fix(ci): fix Dockerfile restore + frontend npm install
26c92bc  fix(ci): switch from az acr build to docker build+push — ACR Tasks blocked
5482871  feat: Angular 19 upgrade + deploy pipeline + startup DB migration
```

---

## Architecture — Clean / Onion (Modular Monolith)

```
TalentBridge
│
├── src/API/TalentBridge.API               ← Presentation: controllers + Angular SPA (wwwroot)
│
├── src/Modules/Identity/
│   ├── Domain                             ← Entities, value objects (no deps)
│   ├── Application                        ← RegisterCommand, LoginCommand
│   └── Infrastructure                     ← EF Core, JWT, password hashing
│
├── src/Modules/Jobs/
│   ├── Domain                             ← Job aggregate, status transitions
│   ├── Application                        ← PostJobCommand, PublishJobCommand, SearchJobsQuery
│   └── Infrastructure                     ← EF Core, SQL migrations
│
├── src/Modules/Applications/
│   ├── Domain                             ← JobApplication aggregate
│   ├── Application                        ← ApplyCommand, UpdateStatusCommand
│   └── Infrastructure                     ← EF Core, HybridCache
│
└── src/Modules/Notifications/
    └── Infrastructure                     ← Outbox relay, Azure Service Bus
```

Each module follows **Domain → Application → Infrastructure** — dependencies point inward only. Controllers touch only the Application layer via MediatR.

---

## Happy Path Walkthrough (curl against live API)

### 1. Register HR
```
POST /api/auth/register
{"email":"hr.demo@talentbridge.dev","password":"Hr@Demo123","role":"CompanyHR"}
→ 201 Created
```

### 2. Register Candidate
```
POST /api/auth/register
{"email":"candidate.demo@talentbridge.dev","password":"Cand@Demo123","role":"Candidate"}
→ 201 Created
```

### 3. Login as HR → JWT
```
POST /api/auth/login
{"email":"hr.demo@talentbridge.dev","password":"Hr@Demo123"}
→ 200 OK
{"token":"eyJhbGci...","expiresAt":"2026-06-23T15:02:19Z","userRole":"CompanyHR"}
```

### 4. Login as Candidate → JWT
```
POST /api/auth/login
{"email":"candidate.demo@talentbridge.dev","password":"Cand@Demo123"}
→ 200 OK
{"token":"eyJhbGci...","userRole":"Candidate"}
```

### 5. HR Posts a Job
```
POST /api/jobs   (Authorization: Bearer <HR JWT>)
{"title":"Senior .NET Developer","location":"Mumbai, India (Hybrid)","salaryMin":2500000,"salaryMax":4000000,...}
→ 201 Created
{"jobId":"cf2d12c6-0fbe-42a7-8924-40e67397b02d","status":"Draft"}
```

### 6. HR Publishes the Job
```
POST /api/jobs/cf2d12c6-.../publish?companyId=...   (Authorization: Bearer <HR JWT>)
→ 204 No Content

GET /api/jobs/cf2d12c6-...
→ 200 OK
{"title":"Senior .NET Developer","status":1,"publishedAtUtc":"2026-06-23T07:03:30Z"}
```

### 7. Candidate Applies
```
POST /api/applications   (Authorization: Bearer <Candidate JWT>)
{"jobId":"cf2d12c6-...","candidateId":"4eedb273-...","coverLetter":"...","resumeUrl":"..."}
→ 201 Created
{"applicationId":"978e071c-ffa6-45fa-83e8-995b7ad16a8d","status":"Submitted"}
```

### 8. HR Moves to UnderReview
```
PATCH /api/applications/978e071c-.../status   (Authorization: Bearer <HR JWT>)
{"newStatus":"UnderReview"}
→ 204 No Content
```

### 9. HR Shortlists the Candidate
```
PATCH /api/applications/978e071c-.../status   (Authorization: Bearer <HR JWT>)
{"newStatus":"Shortlisted"}
→ 204 No Content
```

### Results

| Step              | Endpoint                            | HTTP        |
|-------------------|-------------------------------------|-------------|
| Register HR       | POST /api/auth/register             | 201         |
| Register Candidate| POST /api/auth/register             | 201         |
| Login HR          | POST /api/auth/login                | 200 + JWT   |
| Login Candidate   | POST /api/auth/login                | 200 + JWT   |
| Post Job          | POST /api/jobs                      | 201 (Draft) |
| Publish Job       | POST /api/jobs/{id}/publish         | 204         |
| Candidate Applies | POST /api/applications              | 201 (Submitted) |
| HR → UnderReview  | PATCH /api/applications/{id}/status | 204         |
| HR → Shortlisted  | PATCH /api/applications/{id}/status | 204         |

**9/9 operations succeeded on live Azure.**

---

## What I Learned

1. Azure App Service Free tier is Windows-based — Docker container deployment fails with "Bad Request". Zip deploy works on any tier.
2. Deployment tools can report success even when deployment fails. `swa@1.1.10` exited code 0 while deploying nothing — always verify with `file` on the binary or a live endpoint check.
3. Frontend and backend contracts must match exactly. `.NET System.Text.Json` will not coerce a JSON number into a C# `string` — Angular was sending `role: 0`, backend expected `"Candidate"`.
4. Debugging contract mismatches requires reading both sides. The error message (`Path: $.role`) only makes sense when you see both the Angular payload and the C# record together.
5. Bundling Angular into .NET App Service `wwwroot` with `apiUrl: ''` means same-origin API calls — no CORS, no separate host, one deployment.

---

## What Would Break This

1. **SQL / Managed Identity access removed** — `MigrateAsync()` swallows the error, app boots but every DB write returns 500.
2. **`AZURE_CREDENTIALS` secret expires** — CI/CD stops deploying silently, app drifts from the codebase.
3. **Frontend moved to a different domain** — `apiUrl: ''` is same-origin only; every API call breaks with a CORS error.
4. **Frontend sends `role: 0` instead of `"Candidate"`** — .NET JSON deserializer rejects it; registration returns 500.
5. **No branch protection on `main`** — anyone can push directly, skip review, and break the deploy pipeline.
