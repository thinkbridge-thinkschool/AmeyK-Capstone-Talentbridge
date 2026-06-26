# TalentBridge — Enterprise Hiring Platform

> **Full-stack, cloud-native job board** built with **.NET 10 + Angular 19**, deployed on **Azure App Service** using **Managed Identity**, **Azure Blob Storage**, and **Azure Service Bus**.

---

## Live Demo

| | |
|---|---|
| **Live URL** | https://talentbridge-api-amey.azurewebsites.net |
| **Swagger API** | https://talentbridge-api-amey.azurewebsites.net/swagger |

---

## Demo Credentials

### Test Accounts (ready to use on the live site)

| Role | Email | Password | Access |
|------|-------|----------|--------|
| **HR Manager** | `amey@gmail.com` | `Amey@123` | Post jobs, review applications, view resumes |
| **Candidate** | `yash@gmail.com` | `Yash@123` | Browse jobs, apply, upload resume |
| **HR (Seeded)** | `hr@talentbridge.com` | `HR@1234` | Same as HR above |
| **Candidate (Seeded)** | `candidate@talentbridge.com` | `Candidate@1234` | Same as Candidate above |
| **Admin (Seeded)** | `admin@talentbridge.com` | `Admin@1234` | Full platform access |

> **Quick start:** Log in as `yash@gmail.com` → Browse Jobs → Apply with a PDF resume → Log out → Log in as `amey@gmail.com` → Dashboard → View the application → Click **View Resume**

---

## What It Does

TalentBridge is a **complete hiring platform** where:

- **Candidates** register, browse job listings with filters (keyword, location, salary), apply with a resume (PDF/DOC/DOCX up to 5MB), and track application status
- **HR Managers** post jobs, publish/close them, review incoming applications, update status (Under Review → Shortlisted → Accepted/Rejected), and download candidate resumes
- **Admins** manage the full platform including approving companies
- **Resumes** are stored in **Azure Blob Storage** (private container), accessed via time-limited **User Delegation SAS URLs**
- **Notifications** flow async via the **Transactional Outbox Pattern** → Azure Service Bus → background consumer

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | Angular 19 · Standalone components · Tailwind CSS · TypeScript |
| **Backend** | ASP.NET Core 10 · Clean Architecture · DDD · CQRS with MediatR |
| **Database** | Azure SQL (EF Core 10, code-first migrations per module) |
| **Storage** | Azure Blob Storage (Managed Identity + User Delegation SAS) |
| **Messaging** | Azure Service Bus (Transactional Outbox Pattern) |
| **Caching** | HybridCache (L1 in-process + L2 distributed) |
| **Auth** | JWT Bearer HS256 · BCrypt passwords |
| **Resilience** | Polly v8 (ConcurrencyLimiter → CircuitBreaker → Retry → Timeout) |
| **CI/CD** | GitHub Actions — build, test, publish, deploy in one workflow |
| **IaC** | Azure Bicep (App Service, SQL, Blob, Service Bus, Key Vault) |
| **Observability** | OpenTelemetry → Azure Application Insights |
| **Rate Limiting** | ASP.NET Core built-in: 100 req/min global, 5 req/15min on auth |

---

## Architecture

### Clean Architecture Layers

```
┌──────────────────────────────────────────────────────────┐
│                    TalentBridge.API                       │
│         Controllers · Program.cs · Polly · Swagger        │
└──────────────┬───────────────────────────────────────────┘
               │ references
┌──────────────▼───────────────────────────────────────────┐
│               Infrastructure Layer (per module)           │
│    EF Core DbContext · Repositories · Azure Services      │
└──────────────┬───────────────────────────────────────────┘
               │ implements interfaces from
┌──────────────▼───────────────────────────────────────────┐
│               Application Layer (per module)              │
│         CQRS Commands · Queries · DTOs · MediatR          │
└──────────────┬───────────────────────────────────────────┘
               │ depends on
┌──────────────▼───────────────────────────────────────────┐
│                Domain Layer (per module)                  │
│      Aggregates · Entities · Events · Value Objects       │
└──────────────┬───────────────────────────────────────────┘
               │ uses shared abstractions from
┌──────────────▼───────────────────────────────────────────┐
│                  TalentBridge.Shared                      │
│   AggregateRoot<T> · Result<T> · OutboxMessage · IDomainEvent │
└──────────────────────────────────────────────────────────┘
```

**Dependency rule**: inner layers never reference outer layers. Infrastructure implements interfaces defined in Application.

---

### Bounded Contexts

```
Identity       → User, JWT, BCrypt, RefreshToken
Jobs           → Job (Draft→Active→Closed), JobSearch, HybridCache
Applications   → JobApplication (6-state machine), Resume Upload, SAS URL
Companies      → Company (Create/Approve/UpdateProfile)
Notifications  → OutboxRelay, ServiceBus Consumer, Idempotency Guard
```

---

### Job State Machine

```
                    Job.Create()
                        │
                        ▼
                    ┌───────┐
                    │ Draft  │◄──── Update()
                    └───┬───┘
                        │ Publish()  [raises JobPublishedEvent]
                        ▼
                    ┌───────┐
                    │ Active │
                    └───┬───┘
                        │ Close()  [raises JobClosedEvent]
                        ▼
                    ┌────────┐
                    │ Closed │
                    └────────┘
```

---

### Application State Machine

```
                    JobApplication.Create()
                            │
                            ▼
                       ┌──────────┐
                       │ Submitted │──────────────────────────────────┐
                       └────┬─────┘                                   │
                            │ StartReview(hrId)                       │
                            ▼                                         │
                      ┌───────────┐                                   │
                      │UnderReview│──────────────────────────────────┐│
                      └─────┬─────┘                                  ││
                            │ Shortlist(hrId)                        ││
                            ▼                                        ││
                      ┌────────────┐                                 ││ Withdraw()
                      │ Shortlisted │───────────────────────────────┐││
                      └──────┬─────┘                                │││
                             │ Accept(hrId)    Reject(hrId, notes)  │││
                             ▼                        ▼             ▼▼▼
                        ┌──────────┐           ┌──────────┐  ┌───────────┐
                        │ Accepted │           │ Rejected │  │ Withdrawn │
                        └──────────┘           └──────────┘  └───────────┘
```

---

### Apply for a Job — Full Current Flow

```
Candidate                Angular Frontend           ASP.NET Core API              Azure
    │                          │                          │                          │
    │── Click "Apply" ─────────►│                          │                          │
    │                          │                          │                          │
    │── Select PDF resume ──────►│                          │                          │
    │                          │── POST /api/resumes ──────►│                          │
    │                          │   /upload (JWT Bearer)    │── UploadAsync ───────────►│ Azure Blob Storage
    │                          │                          │   (Managed Identity)     │ resumes-talentbridge-amey
    │                          │                          │◄── blob URI (private) ───│ (private container)
    │                          │◄── 200 { resumeUrl } ────│                          │
    │                          │                          │                          │
    │── Fill cover letter ──────►│                          │                          │
    │                          │── POST /api/applications ─►│                          │
    │                          │   { jobId, resumeUrl,     │── BEGIN TRANSACTION ──────►│ Azure SQL
    │                          │     coverLetter }         │   INSERT JobApplication   │
    │                          │                          │   INSERT OutboxMessage    │
    │                          │                          │   COMMIT (atomic)         │
    │                          │◄── 201 { applicationId } ─│                          │
    │◄── "Application submitted"│                          │                          │
    │                          │          every 5s         │                          │
    │                          │                          │── SELECT unprocessed ─────►│ OutboxMessages
    │                          │                          │── Publish to topic ────────►│ Azure Service Bus
    │                          │                          │── Mark ProcessedAt ────────►│ OutboxMessages
    │                          │                          │                          │
    │                          │          Service Bus delivers to Notifications Consumer │
    │                          │                          │   (BackgroundService,     │
    │                          │                          │    idempotency guard)     │
```

---

### View Resume — HR Flow

```
HR User                  Angular Frontend           ASP.NET Core API              Azure
   │                          │                          │                          │
   │── Click "View Resume" ───►│                          │                          │
   │                          │── GET /api/applications  │                          │
   │                          │       /{id}/resume ──────►│                          │
   │                          │                          │── GetUserDelegationKey ───►│ Blob Storage
   │                          │                          │◄── delegation key ────────│ (Managed Identity)
   │                          │                          │── Build SAS URL           │
   │                          │                          │   (1 hour expiry, read)   │
   │                          │◄── 200 { url, fileName,  │                          │
   │                          │          fileType }      │                          │
   │◄── PDF opens in new tab ─│ (signed URL, expires 1h) │                          │
```

---

### Transactional Outbox Pattern

```
┌────────────────────────────────────────────────────────────────┐
│                      Single DB Transaction                      │
│                                                                │
│   ApplicationsDbContext.SaveChangesAsync()                      │
│   ├── INSERT JobApplications (Id, JobId, CandidateId, ...)     │
│   └── INSERT OutboxMessages  (Type, Payload, OccurredOnUtc)    │
│                                                                │
└────────────────────────────┬───────────────────────────────────┘
                             │ committed atomically
              ┌──────────────▼──────────────┐
              │     OutboxRelayService       │
              │     (BackgroundService)      │
              │     polls every 5 seconds    │
              └──────────────┬──────────────┘
                             │ SELECT WHERE ProcessedAt IS NULL
              ┌──────────────▼──────────────┐
              │      Azure Service Bus       │
              │  topic: talentbridge-events  │
              └──────────────┬──────────────┘
                             │ at-least-once delivery
              ┌──────────────▼──────────────┐
              │  TalentBridgeEventConsumer   │
              │  (BackgroundService)         │
              │  idempotency guard →         │
              │  InMemoryProcessedMessageStore│
              └─────────────────────────────┘
```

---

### Polly Resilience Pipeline

```
Request
  │
  ▼
ConcurrencyLimiter  [permitLimit: 10, queueLimit: 20]   ← bulkhead
  │
  ▼
CircuitBreaker      [5 failures in 30s → 30s open break]
  │
  ▼
Retry               [3× with exponential backoff from 1s]
  │
  ▼
Timeout             [5s per attempt]
  │
  ▼
Upstream Service
```

---

## Folder Structure

```
TalentBridge/
│
├── .github/
│   └── workflows/
│       ├── ci.yml                          ← runs on every push (build + 23 tests)
│       └── deploy.yml                      ← manual workflow_dispatch → Azure App Service
│
├── docs/
│   ├── SOLUTION-BICEP-IAC.md
│   └── ScreenShots/
│
├── frontend/                               ← Angular 19 SPA (served from API wwwroot)
│   ├── src/
│   │   └── app/
│   │       ├── core/
│   │       │   ├── auth/
│   │       │   │   ├── auth.service.ts     ← login/logout, clears tb_candidate_profile on each
│   │       │   │   ├── token.service.ts    ← JWT decode, localStorage keys (tb_token)
│   │       │   │   └── auth.guard.ts
│   │       │   ├── models/
│   │       │   │   └── user.model.ts
│   │       │   └── services/
│   │       │       ├── job.service.ts      ← handles PagedResult<JobDto> { items, totalCount }
│   │       │       ├── resume.service.ts   ← POST /api/resumes/upload with progress tracking
│   │       │       └── toast.service.ts
│   │       │
│   │       ├── features/
│   │       │   ├── auth/
│   │       │   │   ├── login/              ← demo credential cards, role-based redirect
│   │       │   │   └── register/           ← role selector (Candidate / HR)
│   │       │   ├── jobs/
│   │       │   │   ├── job-list/           ← paginated list (pageSize=9), search + salary filter
│   │       │   │   ├── job-detail/         ← full job detail, Apply button
│   │       │   │   └── job-apply/          ← file upload component + cover letter
│   │       │   ├── applications/
│   │       │   │   └── application-detail/ ← status timeline, View Resume button → SAS URL
│   │       │   ├── dashboard/
│   │       │   │   ├── candidate-dashboard/← my applications, status badges
│   │       │   │   └── hr-dashboard/       ← all applications, status update dropdown
│   │       │   ├── profile/                ← candidate profile editor (cached in localStorage)
│   │       │   ├── companies/
│   │       │   └── notifications/
│   │       │
│   │       └── shared/
│   │           └── components/
│   │               └── file-upload/        ← drag & drop, progress bar, 5MB guard, error display
│   │
│   ├── proxy.conf.json                     ← /api/* → localhost API in dev
│   └── angular.json
│
├── infra/                                  ← Azure Bicep IaC
│   ├── main.bicep
│   ├── deploy.sh
│   ├── modules/
│   │   ├── appinsights.bicep
│   │   ├── appservice.bicep
│   │   ├── keyvault.bicep
│   │   ├── servicebus.bicep
│   │   ├── sql.bicep
│   │   └── storage.bicep
│   └── parameters/
│       ├── dev.bicepparam
│       └── prod.bicepparam
│
├── src/
│   │
│   ├── API/
│   │   └── TalentBridge.API/
│   │       ├── Controllers/
│   │       │   ├── ApplicationsController.cs
│   │       │   ├── AuthController.cs
│   │       │   ├── JobsController.cs
│   │       │   ├── NotificationsController.cs
│   │       │   └── ResumesController.cs
│   │       ├── Infrastructure/
│   │       │   └── ManagedCredential.cs    ← DefaultAzureCredential (az login local / MI in Azure)
│   │       ├── Resilience/
│   │       │   ├── ResilienceEndpoints.cs
│   │       │   └── TalentBridgeResiliencePolicies.cs
│   │       ├── Telemetry/
│   │       │   └── TalentBridgeDiagnostics.cs
│   │       ├── DataSeeder.cs               ← seeds 3 users, 1 company, 3 jobs on first boot
│   │       ├── Program.cs                  ← composition root, CSP headers, single BlobServiceClient
│   │       ├── appsettings.json            ← prod config (Storage:ServiceUri, SQL, ServiceBus)
│   │       └── appsettings.Development.json
│   │
│   ├── Shared/
│   │   └── TalentBridge.Shared/
│   │       ├── Common/
│   │       │   └── Result.cs               ← Result<T> + non-generic Result (railway-oriented)
│   │       ├── Domain/
│   │       │   ├── AggregateRoot.cs        ← AggregateRoot<TId>, RaiseDomainEvent(), ClearEvents()
│   │       │   ├── BaseEntity.cs
│   │       │   └── IDomainEvent.cs         ← : INotification (MediatR integration)
│   │       └── Outbox/
│   │           └── OutboxMessage.cs        ← Id, Type, Payload, OccurredOnUtc, ProcessedOnUtc
│   │
│   └── Modules/
│       │
│       ├── Identity/
│       │   ├── TalentBridge.Identity.Domain/
│       │   │   ├── Entities/
│       │   │   │   └── User.cs             ← Create, BCrypt hash, RefreshToken, RevokeToken
│       │   │   ├── Enums/
│       │   │   │   └── UserRole.cs         ← Candidate, CompanyHR, Admin
│       │   │   ├── Events/
│       │   │   │   └── UserRegisteredEvent.cs
│       │   │   └── Repositories/
│       │   │       └── IUserRepository.cs
│       │   ├── TalentBridge.Identity.Application/
│       │   │   ├── Commands/Login/
│       │   │   ├── Commands/Register/
│       │   │   └── Queries/GetMe/          ← GET /api/identity/me → used by profile page
│       │   └── TalentBridge.Identity.Infrastructure/
│       │       ├── Migrations/
│       │       ├── Persistence/
│       │       │   ├── IdentityDbContext.cs
│       │       │   └── UserRepository.cs
│       │       └── Services/
│       │           ├── CurrentUserService.cs
│       │           └── TokenService.cs     ← JWT HS256 sign/decode, refresh token rotation
│       │
│       ├── Companies/
│       │   ├── TalentBridge.Companies.Domain/
│       │   │   ├── Entities/
│       │   │   │   └── Company.cs          ← Create, Approve, UpdateProfile
│       │   │   └── Events/
│       │   │       ├── CompanyCreatedEvent.cs
│       │   │       └── CompanyApprovedEvent.cs
│       │   ├── TalentBridge.Companies.Application/
│       │   │   └── Commands/CreateCompany/
│       │   └── TalentBridge.Companies.Infrastructure/
│       │       └── Persistence/CompanyDbContext.cs
│       │
│       ├── Jobs/
│       │   ├── TalentBridge.Jobs.Domain/
│       │   │   ├── Aggregates/
│       │   │   │   └── Job.cs              ← Result<Job>.Create, Draft→Active→Closed
│       │   │   ├── Enums/
│       │   │   │   ├── JobStatus.cs        ← Draft, Active, Closed
│       │   │   │   └── JobType.cs
│       │   │   ├── Events/
│       │   │   │   ├── JobCreatedEvent.cs
│       │   │   │   ├── JobPublishedEvent.cs
│       │   │   │   └── JobClosedEvent.cs
│       │   │   └── Repositories/
│       │   │       └── IJobRepository.cs
│       │   ├── TalentBridge.Jobs.Application/
│       │   │   ├── Commands/PostJob/
│       │   │   ├── Commands/PublishJob/
│       │   │   ├── Commands/CloseJob/
│       │   │   ├── DTOs/
│       │   │   │   ├── JobDto.cs
│       │   │   │   └── PagedResult.cs      ← record PagedResult<T>(List<T> Items, int TotalCount)
│       │   │   └── Queries/
│       │   │       ├── GetJobById/
│       │   │       └── SearchJobs/
│       │   │           ├── SearchJobsQuery.cs        ← IRequest<PagedResult<JobDto>>
│       │   │           └── SearchJobsQueryHandler.cs ← cache all results, Skip/Take in memory
│       │   └── TalentBridge.Jobs.Infrastructure/
│       │       ├── Migrations/
│       │       └── Persistence/
│       │           ├── JobRepository.cs
│       │           └── JobsDbContext.cs
│       │
│       ├── Applications/
│       │   ├── TalentBridge.Applications.Domain/
│       │   │   ├── Aggregates/
│       │   │   │   └── JobApplication.cs   ← 6-state machine, guards invalid transitions
│       │   │   ├── Enums/
│       │   │   │   └── ApplicationStatus.cs ← Submitted, UnderReview, Shortlisted,
│       │   │   │                               Accepted, Rejected, Withdrawn
│       │   │   ├── Events/
│       │   │   │   ├── ApplicationSubmittedEvent.cs
│       │   │   │   ├── ApplicationStatusChangedEvent.cs
│       │   │   │   ├── ApplicationAcceptedEvent.cs
│       │   │   │   └── ApplicationWithdrawnEvent.cs
│       │   │   └── Repositories/
│       │   │       └── IApplicationRepository.cs
│       │   ├── TalentBridge.Applications.Application/
│       │   │   ├── Commands/Apply/          ← saves application + OutboxMessage atomically
│       │   │   ├── Commands/UpdateStatus/
│       │   │   ├── Commands/UploadResume/
│       │   │   ├── Interfaces/
│       │   │   │   └── IResumeStorageService.cs ← UploadAsync, GenerateSasUrlAsync, Delete
│       │   │   └── Queries/GetApplication/
│       │   └── TalentBridge.Applications.Infrastructure/
│       │       ├── Migrations/
│       │       ├── Persistence/
│       │       │   ├── ApplicationsDbContext.cs
│       │       │   └── ApplicationRepository.cs
│       │       ├── Storage/
│       │       │   ├── AzureResumeStorageService.cs  ← private container, User Delegation SAS
│       │       │   └── LocalResumeStorageService.cs  ← wwwroot/uploads/ for local dev
│       │       └── DependencyInjection.cs   ← selects Azure or Local based on Storage:ServiceUri
│       │
│       └── Notifications/
│           ├── TalentBridge.Notifications.Domain/
│           │   └── Entities/
│           │       └── NotificationRecord.cs
│           ├── TalentBridge.Notifications.Application/
│           │   └── Queries/GetNotifications/
│           └── TalentBridge.Notifications.Infrastructure/
│               ├── Consumers/
│               │   └── TalentBridgeEventConsumer.cs  ← BackgroundService, at-least-once safe
│               ├── Relay/
│               │   ├── OutboxRelayService.cs          ← polls DB every 5s, publishes to SB
│               │   ├── OutboxRepository.cs
│               │   └── RelayDbContext.cs
│               └── Services/
│                   └── InMemoryProcessedMessageStore.cs ← ConcurrentDictionary idempotency
│
├── tests/
│   ├── TalentBridge.Jobs.Domain.Tests/
│   │   └── JobTests.cs                     ← 8 tests: state machine, validation guards
│   ├── TalentBridge.Applications.Domain.Tests/
│   │   └── JobApplicationTests.cs          ← 8 tests: full 6-state lifecycle
│   └── TalentBridge.Identity.Domain.Tests/
│       └── UserTests.cs                    ← 7 tests: create, BCrypt, refresh token, revoke
│
├── Dockerfile
├── TalentBridge.slnx                       ← 20 projects, 0 errors, 23/23 tests passing
└── README.md
```

---

## Module Overview

| Module | Aggregate / Entity | Key Operations | Storage |
|--------|-------------------|----------------|---------|
| **Identity** | `User` | Register, Login, JWT, RefreshToken | Azure SQL |
| **Jobs** | `Job` | Post, Publish, Close, Search (paginated, cached) | Azure SQL + HybridCache |
| **Applications** | `JobApplication` | Apply, UpdateStatus, UploadResume, ViewResume (SAS) | Azure SQL + Blob Storage |
| **Companies** | `Company` | Create, Approve, UpdateProfile | Azure SQL |
| **Notifications** | `NotificationRecord` | Listen to events, store + retrieve notifications | Service Bus + SQL |
| **Shared** | `AggregateRoot<T>`, `Result<T>`, `OutboxMessage` | Used by all modules | — |

---

## API Reference

### Auth (`/api/auth`)
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | `/register` | Anonymous | Register new user (Candidate or HR) |
| POST | `/login` | Anonymous | Login — returns JWT + refresh token |
| POST | `/refresh` | Anonymous | Refresh access token |
| GET | `/me` | Authenticated | Current user profile |

### Jobs (`/api/jobs`)
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | `/` | CompanyHR, Admin | Create a draft job |
| GET | `/{id}` | Anonymous | Get job by ID |
| GET | `/search?keyword=&location=&page=&size=` | Anonymous | Paginated job search |
| POST | `/{id}/publish` | CompanyHR | Publish a draft job |
| POST | `/{id}/close` | CompanyHR, Admin | Close an active job |

### Applications (`/api/applications`)
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | `/` | Candidate | Apply for a job |
| GET | `/{id}` | Authenticated | Get application detail |
| PATCH | `/{id}/status` | CompanyHR, Admin | Update application status |
| GET | `/{id}/resume` | CompanyHR, Admin | Get time-limited SAS URL for resume PDF |

### Resumes (`/api/resumes`)
| Method | Endpoint | Role | Description |
|--------|----------|------|-------------|
| POST | `/upload` | Candidate | Upload resume (PDF/DOC/DOCX, max 5MB) |

### Resilience (`/api/resilience`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/status` | Circuit breaker state |
| POST | `/force-failure/{enabled}` | Toggle failure injection for testing |
| GET | `/test-call` | Make a test HTTP call through the Polly pipeline |

---

## Azure Infrastructure

| Resource | Name | Purpose |
|----------|------|---------|
| App Service | `talentbridge-api-amey` | Hosts the .NET API + Angular SPA |
| Azure SQL | `talentbridge-sql-amey` | 5 databases (one per module) |
| Blob Storage | `talentbridgestamey2` | Resume storage (private container, SAS access) |
| Service Bus | `talentbridge-sb-amey2` | Async events (Outbox relay → Notifications consumer) |
| Key Vault | `talentbridge-kv-amey` | Secrets (JWT signing key, connection strings) |
| App Insights | — | OpenTelemetry traces and logs |

### Managed Identity Role Assignments
| Identity | Role | Scope |
|----------|------|-------|
| App Service | `Storage Blob Data Contributor` | `talentbridgestamey2` storage account |
| App Service | `Azure Service Bus Data Owner` | `talentbridge-sb-amey2` namespace |
| App Service | `Key Vault Secrets User` | `talentbridge-kv-amey` vault |
| App Service | `Contributor` | Azure SQL server |

---

## Key Design Decisions

### 1. Modular Monolith
Five bounded contexts in one deployable unit — each with its own `DbContext`, domain model, and migrations. No shared tables across modules. Cross-module access goes via the API layer.

### 2. Pagination via `PagedResult<T>`
```csharp
public record PagedResult<T>(List<T> Items, int TotalCount);
```
`SearchJobsQueryHandler` fetches all matching jobs (cached with HybridCache), then applies `Skip/Take` in memory so both total count and page slice are always correct without double queries.

### 3. Resume Storage — Private Container + User Delegation SAS
The storage account has `allowBlobPublicAccess = false`. Resumes are stored in a private container. HR resume access uses **User Delegation SAS** (1-hour expiry) generated via the App Service's Managed Identity — no account key or connection string required.

### 4. Outbox Pattern — At-Least-Once Delivery
The Apply command saves `JobApplication` and `OutboxMessage` in one atomic transaction. `OutboxRelayService` polls the DB every 5 seconds, publishes to Azure Service Bus, and marks records as processed. The consumer uses `InMemoryProcessedMessageStore` to deduplicate redeliveries.

### 5. HybridCache (L1 + L2)
```
Job detail  →  L1: 2 min  /  L2: 10 min
Job search  →  L1: 2 min  /  L2: 5 min  (cached per filter combo, page applied in memory)
```

### 6. Profile Cache Isolation Between Users
On every **login** and **logout**, `localStorage.removeItem('tb_candidate_profile')` is called in `AuthService` so a new user never sees a previous user's cached name or skills.

### 7. Security Headers
CSP is tuned to allow Tailwind CSS CDN (loaded at runtime in `index.html`), Google Fonts, and the Blob Storage domain for SAS URL access, while blocking all other external sources.

---

## Running Locally

```bash
# Prerequisites: .NET 10 SDK, Node 20, SQL Server (LocalDB or Docker)

# 1. Clone
git clone https://github.com/thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge.git
cd TalentBridge

# 2. Set your local connection string in:
#    src/API/TalentBridge.API/appsettings.Development.json

# 3. Run the API (auto-migrates + seeds demo data on first boot)
cd src/API/TalentBridge.API
dotnet run

# 4. Run the Angular frontend (separate terminal)
cd frontend
npm install
npm start       # proxies /api/* to the local API

# Swagger UI → https://localhost:7xxx/swagger
```

---

## Tests

```bash
dotnet test
```

| Suite | Tests | What's Covered |
|-------|-------|----------------|
| `TalentBridge.Jobs.Domain.Tests` | 8 | Job state machine, validation guards, `IsAcceptingApplications` |
| `TalentBridge.Applications.Domain.Tests` | 8 | Full 6-state machine: submit, review, shortlist, accept, reject, withdraw |
| `TalentBridge.Identity.Domain.Tests` | 7 | User: create, events, RefreshToken, RevokeToken, BCrypt verify |
| **Total** | **23** | **23 passed, 0 failed** |

---

## CI/CD

**`ci.yml`** — triggers on every push: restore → build → test → report

**`deploy.yml`** — manual `workflow_dispatch`:
1. Build Angular production bundle
2. Publish .NET API
3. Copy Angular `dist/` → `wwwroot/` inside the publish folder
4. Azure login via GitHub OIDC
5. Deploy to App Service
6. Print live URL

```bash
# Trigger manually from CLI
gh workflow run "TalentBridge — Full Stack Deploy" \
  --repo thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge \
  --ref main \
  -f environment=prod
```

---

## Security Headers (every response)

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.tailwindcss.com;
  style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
  font-src 'self' data: https://fonts.gstatic.com;
  img-src 'self' data: blob:;
  connect-src 'self' https://talentbridgestamey2.blob.core.windows.net

X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Referrer-Policy: strict-origin-when-cross-origin
Strict-Transport-Security: max-age=31536000; includeSubDomains
```

---

## Project Stats

| Metric | Value |
|--------|-------|
| Solution projects | 20 |
| Build warnings | 0 |
| Build errors | 0 |
| Unit tests | 23 (all passing) |
| Bounded contexts | 5 |
| Azure resources | 6 |
| Angular features | 7 (auth, jobs, applications, dashboard, profile, companies, notifications) |
| Live since | June 2026 |
