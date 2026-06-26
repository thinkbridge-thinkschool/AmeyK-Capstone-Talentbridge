# TalentBridge — Project Submission (Day 30)

**Submitted by:** Amey Khot  
**GitHub:** https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge  
**Live PR:** https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge/pull/9

---

## What is TalentBridge?

TalentBridge is a full-stack hiring platform where companies post jobs and candidates apply. HR managers review applications, move them through a hiring pipeline (Submitted → Under Review → Shortlisted → Accepted/Rejected), and candidates receive real-time notifications at every step.

---

## How to Run Locally

### Prerequisites
- .NET 10 SDK
- Node.js 20+
- SQL Server (or use the InMemory test configuration)
- Azure CLI (`az login`) for Blob/Service Bus in dev

### Backend
```bash
cd src/API/TalentBridge.API
dotnet run
# API available at https://localhost:7001
```

### Frontend
```bash
cd frontend
npm install
ng serve
# App available at http://localhost:4200
```

### Run All Tests
```bash
dotnet test TalentBridge.slnx
# 32 tests pass — 0 failures
```

---

## Demo Accounts

Log in at `http://localhost:4200` with any of these:

| Role | Email | Password | What you can do |
|---|---|---|---|
| Admin | admin@talentbridge.com | Admin@1234 | Manage users, approve companies, view everything |
| HR Manager | hr@talentbridge.com | HR@1234 | Post jobs, review applicants, update status |
| Candidate | candidate@talentbridge.com | Candidate@1234 | Search jobs, apply, track application status |

Seed data includes **3 published jobs** and **2 sample applications** ready to explore.

---

## Features

### For Candidates
- Browse and search jobs by keyword, location, and salary range
- Apply with a cover letter and resume URL
- Track all applications and their current status
- Withdraw an application at any time
- Get notifications when your application status changes (shortlisted, accepted, rejected)
- Edit your profile (bio, skills, LinkedIn, GitHub, resume link)

### For HR Managers
- Create job postings (saved as Draft, then Published)
- View all applicants for each job
- Move applicants through the pipeline: Submitted → Under Review → Shortlisted → Accepted
- Reject with a reason at any point
- Close job listings when filled

### For Admins
- Approve company profiles
- Deactivate user accounts
- View all users and companies

---

## Application Status Flow

```
Submitted
    │
    ▼
Under Review ──► Rejected
    │
    ▼
Shortlisted ──► Rejected
    │
    ▼
Accepted

(Candidate can Withdraw at any point)
```

---

## Architecture

TalentBridge is built as a **Modular Monolith** — five independent bounded contexts inside one deployable API. Each module owns its own database tables and never directly accesses another module's data.

```
┌─────────────────────────────────────────────────────────┐
│                  Angular 19 Frontend                    │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP / REST
┌────────────────────────▼────────────────────────────────┐
│               ASP.NET Core 10 API                       │
│  ┌──────────┐ ┌──────┐ ┌────────────┐ ┌─────────────┐  │
│  │ Identity │ │ Jobs │ │Applications│ │Notifications│  │
│  └──────────┘ └──────┘ └────────────┘ └─────────────┘  │
│  ┌───────────┐                                          │
│  │ Companies │                                          │
│  └───────────┘                                          │
└─────────────────────────┬───────────────────────────────┘
                          │
       ┌──────────────────┼──────────────────┐
       ▼                  ▼                  ▼
  Azure SQL DB    Azure Service Bus    Azure Blob Storage
  (5 contexts)   (event messaging)    (resume uploads)
```

### Key Patterns Used
| Pattern | Where |
|---|---|
| CQRS + MediatR | Every feature is a Command or Query handler |
| Domain Events + Outbox | Status changes publish events atomically with the DB write |
| Railway-Oriented Result\<T\> | No exceptions for business errors — explicit success/failure |
| Repository-free (EF Core) | DbContext used directly in handlers — no unnecessary abstraction |
| JWT HS256 + Refresh Tokens | Stateless auth with 15-min access / 7-day refresh token pair |
| HybridCache | 2-min in-process + 10-min distributed cache for job search |
| Polly Resilience Pipeline | Circuit breaker + retry + timeout on outbound calls |

---

## Tech Stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core 10, C# 13, .NET 10 |
| ORM | Entity Framework Core 10 |
| Messaging | MediatR v14, FluentValidation |
| Auth | JWT Bearer, BCrypt.Net (work factor 11) |
| Cloud | Azure App Service, Azure SQL, Azure Service Bus, Azure Blob, Azure Static Web Apps |
| Frontend | Angular 19, TypeScript 5.6, RxJS, Tailwind CSS |
| Testing | xUnit, FluentAssertions, WebApplicationFactory, EF InMemory |
| CI/CD | GitHub Actions with Cobertura coverage report |
| IaC | Azure Bicep |
| Observability | OpenTelemetry → Azure Application Insights |

---

## API Reference

### Authentication
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/identity/register` | POST | Public | Create account |
| `/api/identity/login` | POST | Public | Get JWT token |
| `/api/identity/refresh` | POST | Public | Refresh token |
| `/api/identity/me` | GET | Bearer | Current user info |
| `/api/identity/profile` | PATCH | Bearer | Update profile |

### Jobs
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/jobs/search` | GET | Public | Search/filter jobs |
| `/api/jobs/{id}` | GET | Public | Job detail |
| `/api/jobs` | POST | HR/Admin | Create job |
| `/api/jobs/{id}/publish` | POST | HR/Admin | Publish a draft job |
| `/api/jobs/{id}/close` | PATCH | HR/Admin | Close job listing |
| `/api/jobs/{id}` | PUT | HR/Admin | Update job |
| `/api/jobs/{id}` | DELETE | HR/Admin | Delete job |
| `/api/jobs/mine` | GET | HR/Admin | My job postings |

### Applications
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/applications` | POST | Candidate | Apply to a job |
| `/api/applications/my` | GET | Candidate | My applications |
| `/api/applications/{id}` | GET | Bearer | Application detail |
| `/api/applications/{id}/status` | PATCH | HR/Admin | Update status |
| `/api/applications/{id}/withdraw` | PATCH | Candidate | Withdraw application |
| `/api/applications/{id}/history` | GET | Bearer | Full status history |

### Notifications
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/notifications` | GET | Bearer | All notifications |
| `/api/notifications/{id}/read` | PATCH | Bearer | Mark as read |

### Other
| Endpoint | Method | Auth | Description |
|---|---|---|---|
| `/api/resumes/upload` | POST | Candidate | Upload resume to Azure Blob |
| `/api/companies` | POST | HR/Admin | Create company |
| `/api/admin/users` | GET | Admin | All users |
| `/api/admin/users/{id}/deactivate` | PATCH | Admin | Deactivate user |
| `/api/admin/companies/{id}/approve` | PATCH | Admin | Approve company |

---

## CI Run (Green)

**URL:** https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge/actions/runs/28173350638/job/83443067337

![Test Results](docs/ScreenShots/test-results-32-pass.png)

---

## Test Coverage at Each Layer

### Unit (23 tests, ~20s, no I/O)

| Suite | Tests | What is covered |
|---|---|---|
| `TalentBridge.Identity.Domain.Tests` | 7 | User aggregate — create, BCrypt hash, refresh token issue/revoke, duplicate email guard |
| `TalentBridge.Jobs.Domain.Tests` | 8 | Job aggregate state machine — Draft→Active→Closed, validation guards, IsAcceptingApplications |
| `TalentBridge.Applications.Domain.Tests` | 8 | JobApplication aggregate — all 6 status transitions (Submitted→UnderReview→Shortlisted→Accepted/Rejected, Withdraw), invalid transition guards |

**Total: 23 tests — all green**

Coverage (Coverlet / Cobertura):

| Domain Layer | Line | Branch |
|---|---|---|
| `Identity.Domain` | 65.4% | 75.0% |
| `Jobs.Domain` | 79.4% | 37.5% |
| `Applications.Domain` | 74.5% | 55.0% |

---

### Integration (9 tests, WebApplicationFactory + InMemory EF Core)

| Suite | Tests | What is covered |
|---|---|---|
| `IdentityEndpointTests` | 4 | Register, duplicate email → 400, login → JWT, wrong password → 401 |
| `JobsEndpointTests` | 3 | GET /search (public → 200), POST no-auth → 401, POST as HR → 201 |
| `HappyPathE2ETests` | 1 | Full 12-step hiring flow (see below) |

**Total: 9 tests — all green**

Full stack line coverage (all 5 modules): **26.6%** — lower because it exercises one happy path; uncovered lines are error branches and admin-only flows.

**E2E flow (HappyPathE2ETests — 12 steps):**
Register HR → Login → Get hrId → Post Job → Register Candidate → Login → Get candidateId → Search Jobs → Apply → HR moves to UnderReview → HR Shortlists → Candidate sees status = "Shortlisted"

---

## Hot-Path p99 Before/After Polish

**GET /api/jobs/search** (most-called public endpoint, backed by HybridCache)

```
Benchmark: 30 cache-miss requests (unique ?keyword=benchN forces DB hit)
       vs  30 cache-hit  requests (same URL, L1 HybridCache already warm)
Measured with System.Diagnostics.Stopwatch against live Azure SQL-backed API.
```

| | avg | p50 | p95 | p99 |
|---|---|---|---|---|
| **Before** — cache MISS, every request hits Azure SQL | 284.7 ms | 278 ms | 343 ms | 348 ms |
| **After** — cache HIT, served from L1 in-process HybridCache | 83.3 ms | 81 ms | 94 ms | **110 ms** |
| **Improvement** | **3.4×** | **3.4×** | **3.6×** | **3.2×** |

**What changed (Before → After):**

Before: `JobsController → SearchJobsQueryHandler → JobsDbContext.Jobs.Where(...).ToListAsync()` — full Azure SQL round-trip on every request

After: `JobsController → SearchJobsQueryHandler → HybridCache.GetOrCreateAsync(key, ...)` — L1 in-process cache (2-min TTL) serves from memory; L2 distributed (10-min TTL) survives restarts

Cold path: ~285 ms → ~83 ms avg. p99 drops from 348 ms to 110 ms.

---

## Cloud Infrastructure (Azure)

All resources provisioned via Bicep IaC in `infra/`:

| Resource | Role |
|---|---|
| Azure App Service | Hosts the .NET API |
| Azure SQL Database | Persistent storage (separate schema per module) |
| Azure Service Bus | Async event delivery (application status changes → notifications) |
| Azure Blob Storage | Resume file storage (5 MB max, PDF/DOC/DOCX) |
| Azure Static Web Apps | Hosts the Angular SPA |
| Azure Application Insights | Distributed traces, logs, exceptions |
| Azure Key Vault | Secrets management |

All services authenticate via **Managed Identity** — no passwords in code or config files.

---

## CI/CD

Every push and pull request to `main` runs:

1. Restore NuGet packages
2. Build in Release mode
3. Run all 32 tests with code coverage (Cobertura)
4. Upload coverage report as a GitHub artifact

---

## Submission PRs

| PR | Description |
|---|---|
| [#9 — day-31-polish-tests-perf-security](https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge/pull/9) | Integration tests, E2E happy path, CI coverage collection |
| #8 — feature/angular-frontend | Full Angular 19 SPA (all feature modules) |
| Earlier PRs | Core backend: modular monolith, auth, jobs, applications, companies, notifications, Azure deployment, Bicep IaC |
