# Day 28 — Design Review + Architecture Decision Records

**Author:** amey2612  
**Date:** 2026-06-22  
**Branch:** day-28-adr  
**Repo:** thinkbridge-thinkschool/AmeyK-Capstone-Talentbridge  

---

## What This Day Builds

- Five Architecture Decision Records (ADR-0001 through ADR-0005) covering the
  most important design choices in TalentBridge
- A day-by-day build plan showing what was built and why in each session
- A mentor/peer critique of the design and how it changed an architectural decision

---

## Part 1 — Architecture Decision Records

All ADRs live in `docs/adr/`. Each follows the same structure: Status, Date,
Context, Decision, Alternatives Considered, Consequences.

### ADR-0001 — Modular Monolith over Microservices
**File:** `docs/adr/0001-modular-monolith-over-microservices.md`  
**Status:** Accepted

**The decision that matters most in this capstone.**

Five business domains (Identity, Jobs, Applications, Companies, Notifications),
one developer, six days. The choice was between Microservices and a Modular
Monolith.

**Key trade-off:**  
Microservices give independent deployability and failure isolation per domain.
But each service needs its own container, pipeline, service discovery, and
distributed transaction strategy. For one developer on a six-day timeline, that
overhead consumes the entire project before a single feature ships.

**The decision:**  
Single ASP.NET Core process. Each domain is a fully isolated module with its
own DbContext, its own MediatR handlers, and zero direct code references to
other module internals. Cross-module communication happens only through domain
events over Azure Service Bus (see ADR-0004). This gives clean boundaries now
and a clear extraction path if a module needs to become its own service later.

**Alternatives rejected:**  
Microservices (infra overhead), Single-layer monolith (tight coupling, no
migration path), Vertical slice (can't enforce domain invariants across features).

---

### ADR-0002 — Outbox Pattern for Domain Event Publishing
**File:** `docs/adr/0002-outbox-pattern-for-domain-events.md`  
**Status:** Accepted

Domain events (ApplicationSubmittedEvent, JobPublishedEvent, etc.) must reach
Azure Service Bus reliably. Directly publishing inside a command handler creates
a two-phase commit problem: the database write and the Service Bus send are two
separate I/O operations with no atomic guarantee.

**The decision:**  
Write the event to the `OutboxMessages` table in the same SQL transaction as the
business entity change. A `BackgroundService` (`OutboxRelayService`) polls every
5 seconds, sends pending rows to Service Bus, and stamps `ProcessedOnUtc` after
a successful send. Delivery is at-least-once; consumers must be idempotent.

**Alternatives rejected:**  
Direct publish (no atomicity guarantee), Event sourcing (too complex for the
timeline), CDC via SQL Server (extra Azure resource), MediatR in-process only
(no durability across restarts).

---

### ADR-0003 — JWT Bearer over Session-Based Authentication
**File:** `docs/adr/0003-jwt-bearer-over-session-authentication.md`  
**Status:** Accepted

Three roles (HR, Candidate, Admin) across all five modules. Every protected
endpoint needs the caller's identity and role without a cross-module database
roundtrip.

**The decision:**  
HMAC-SHA256 JWT, 8-hour expiry, three claims (user GUID, email, role). Secret
stored in Azure Key Vault, never in source control. Registered once on the
shared ASP.NET Core pipeline so every module's `[Authorize(Roles = "...")]`
works automatically. `User` entity has `RefreshToken` fields ready for a future
shorter-expiry refresh flow.

**Alternatives rejected:**  
Cookie/session (requires Redis for multi-instance, adds per-request lookup),
Azure AD B2C (2–3 days of setup cost), API keys (no expiry, no role claims),
OAuth2 code flow (no redirect step in this direct login model).

---

### ADR-0004 — Azure Service Bus over Direct API Calls
**File:** `docs/adr/0004-azure-service-bus-over-direct-api-calls.md`  
**Status:** Accepted

Modules must react to each other's state changes (submit application → send
email, publish job → send alerts) without direct code dependencies.

**The decision:**  
Azure Service Bus topic `talentbridge-events` with a `notifications` subscription.
`OutboxRelayService` publishes (tied to ADR-0002). `TalentBridgeEventConsumer`
subscribes with `MaxConcurrentCalls = 5`, `AutoCompleteMessages = false`, and
an `IProcessedMessageStore` idempotency guard. No module has a project reference
to another module.

**Alternatives rejected:**  
Direct HTTP/DI calls (Notifications outage fails Application submission), MediatR
in-process only (no durability, no extraction path), Azure Event Grid (HTTP
webhook model, weaker dead-letter semantics), Azure Storage Queues (point-to-point,
cannot fan-out to multiple subscribers).

---

### ADR-0005 — Azure SQL over PostgreSQL
**File:** `docs/adr/0005-azure-sql-over-postgresql.md`  
**Status:** Accepted

Five module DbContexts all need a relational database. Azure SQL and Azure
Database for PostgreSQL Flexible Server were both viable.

**The decision:**  
Azure SQL with `Authentication=Active Directory Default`. No passwords anywhere
— Managed Identity provides credentials locally (`az login`) and in Azure (App
Service MI). Server provisioned with `publicNetworkAccess: Disabled` and a
private endpoint on `privatelink.database.windows.net`. All five module
migrations are SQL Server migrations.

**Alternatives rejected:**  
PostgreSQL (requires custom Npgsql token provider plugin for Managed Identity,
different private DNS zone, would need all five module migrations regenerated),
SQLite (single-writer lock, no production use), Cosmos DB (relational domain
model, outbox atomic transactions are simpler on SQL).

---

## Part 2 — Day-by-Day Build Plan

| Day | Date | What Was Built |
|-----|------|---------------|
| 22 | 2026-06-12 | Solution scaffolding — 5-module folder structure (Domain / Application / Infrastructure per module), shared primitives (`AggregateRoot`, `IDomainEvent`, `Result<T>`, `OutboxMessage`), all domain entities and aggregates (`User`, `Job`, `JobApplication`, `Company`, `NotificationRecord`), EF Core DbContexts, MediatR command/query handlers, `TokenService`, `OutboxRelayService`, `TalentBridgeEventConsumer`, Mermaid architecture diagrams in DESIGN.md |
| 23 | 2026-06-15 | CI pipeline — GitHub Actions workflow for `dotnet build` + `dotnet test` on every push to `dev` and PR to `main` |
| 24 | 2026-06-16 | Infrastructure + deployment — Bicep IaC for all Azure resources (Key Vault, Storage, App Insights, Service Bus, Static Web App, VNet), Dockerfile for the API, Azure deployment stacks (dev + prod params), GitHub Actions deploy workflow, Angular frontend SPA, live Azure deploy proof |
| 25 | 2026-06-17 | Managed Identity — removed all hardcoded secrets and connection string passwords. `Authentication=Active Directory Default` for SQL. `DefaultAzureCredential` for Service Bus and Blob Storage. Key Vault for JWT secret. SOLUTION-MI-KEYVAULT.md with screenshots |
| 26 | 2026-06-18 | Observability — OpenTelemetry wired to Azure App Insights. `ActivitySource` spans on `OutboxRelayService` publish loop. SQL client instrumentation. HTTP client instrumentation. Five KQL queries (slow jobs, error rate, outbox lag, user registrations over time, p95 latency). SOLUTION-APPINSIGHTS-KQL.md |
| 27 | 2026-06-19 | Security pass — STRIDE threat model (20 rows across 5 domains), security headers middleware (CSP, HSTS, X-Frame-Options, X-Content-Type-Options, CORP, Referrer-Policy), CORS restricted to known origins, rate limiting (global 100 req/min sliding window + auth 5 req/15min fixed window), API versioning (`Asp.Versioning.Mvc`), Kestrel body limit 10 MB, MIME content-type whitelist on resume upload, SQL private endpoint (`publicNetworkAccess: Disabled`), OWASP ZAP API scan in GitHub Actions CI. SOLUTION-SECURITY.md with screenshots |
| 28 | 2026-06-22 | Architecture Decision Records — ADR-0001 through ADR-0005, design review, mentor critique |

---

## Part 3 — Top Critique + How It Changed the Design

### The Critique

During peer review, the most pointed critique was about the **`InMemoryProcessedMessageStore`**
in the Service Bus consumer.

The reviewer's exact challenge:

> "Your `TalentBridgeEventConsumer` uses an in-memory dictionary to track which
> message IDs have already been processed. That dictionary lives in the process's
> heap. The moment your App Service restarts — a deploy, a crash, a scale-in
> event — that dictionary is gone. Service Bus will redeliver any message that
> was received but not yet completed before the restart. Your consumer will
> process it again with no memory that it already ran. You could send a candidate
> two acceptance emails, or trigger two welcome emails for one registration. This
> is a correctness issue, not just a scalability concern. At-least-once delivery
> from the broker does not protect you if your own idempotency guard resets on
> every restart."

This is valid. The current implementation:

```csharp
// InMemoryProcessedMessageStore — resets on every process restart
public class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly HashSet<string> _processed = new();

    public Task<bool> IsProcessedAsync(string messageId) =>
        Task.FromResult(_processed.Contains(messageId));

    public Task MarkProcessedAsync(string messageId)
    {
        _processed.Add(messageId);
        return Task.CompletedTask;
    }
}
```

### How It Changed the Design

The critique did not change the implementation (adding Redis or a DB table would
have taken a day and moved focus away from the ADR work), but it changed **two
things**:

**1. The interface was already the right abstraction.**  
`IProcessedMessageStore` was extracted as an interface from the first day.
The in-memory implementation is registered in DI as:

```csharp
services.AddSingleton<IProcessedMessageStore, InMemoryProcessedMessageStore>();
```

Replacing it with a persistent implementation requires changing exactly one line
in `DependencyInjection.cs`. No consumer code changes. No `TalentBridgeEventConsumer`
changes. The abstraction absorbed the critique without a rewrite.

**2. ADR-0004 was strengthened to document this explicitly.**  
Before the critique, the negative consequence read as a vague "consumers must be
idempotent." After the critique, the ADR was written to call out specifically:

> "The current implementation of `IProcessedMessageStore` is in-memory
> (`InMemoryProcessedMessageStore`), meaning it resets on process restart and
> does not protect against duplicates across instances. A persistent store
> (Redis or a database table) is required before multi-instance deployment."

The critique converted an unacknowledged risk into a documented, traceable
decision with a concrete remediation path. That is what ADRs are for: not
hiding the imperfections but recording them so the next developer (or the
production-readiness reviewer) knows exactly what to fix and why.

**The fix when production-ready:**

```csharp
// Replace one line in DependencyInjection.cs:
services.AddSingleton<IProcessedMessageStore, RedisProcessedMessageStore>();

// RedisProcessedMessageStore — survives restarts, works across instances
public class RedisProcessedMessageStore : IProcessedMessageStore
{
    private readonly IConnectionMultiplexer _redis;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(25);

    public async Task<bool> IsProcessedAsync(string messageId) =>
        await _redis.GetDatabase().KeyExistsAsync($"processed:{messageId}");

    public async Task MarkProcessedAsync(string messageId) =>
        await _redis.GetDatabase().StringSetAsync(
            $"processed:{messageId}", "1", Ttl);
}
```

No other file changes. The `TalentBridgeEventConsumer` is unaware of the swap.

---

## ADR File Locations

```
docs/adr/
  0001-modular-monolith-over-microservices.md
  0002-outbox-pattern-for-domain-events.md
  0003-jwt-bearer-over-session-authentication.md
  0004-azure-service-bus-over-direct-api-calls.md
  0005-azure-sql-over-postgresql.md
```

---

## Submission Checklist

- [x] ADR-0001 — Modular Monolith over Microservices (the decision that matters most)
- [x] ADR-0002 — Outbox Pattern for domain event publishing
- [x] ADR-0003 — JWT Bearer over session-based authentication
- [x] ADR-0004 — Azure Service Bus over direct API calls
- [x] ADR-0005 — Azure SQL over PostgreSQL
- [x] Day-by-day build plan (Day 22 through Day 28)
- [x] Top critique received (InMemoryProcessedMessageStore resets on restart)
- [x] How it changed the design (ADR-0004 negative consequences made explicit; fix path documented)
