# ADR-0001 — Use Modular Monolith instead of Microservices

**Status:** Accepted  
**Date:** 2026-06-22  
**Author:** amey2612  
**Project:** TalentBridge — Enterprise Hiring Platform  

---

## Context

TalentBridge is an enterprise hiring platform that handles five distinct business
domains: Identity (user registration and authentication), Jobs (job posting and
management), Applications (candidate applications and status tracking), Companies
(company profiles), and Notifications (email and event-driven alerts).

When starting the capstone, a fundamental architectural decision was required:
how should these five domains be deployed and how should they communicate with
each other?

The team size is one developer. The timeline is a capstone project spanning
approximately six days of active development. The deployment target is Azure
with a student subscription that has resource and quota limits. The domains need
to share some cross-cutting concerns like authentication and the database
connection, but must stay logically separated so the codebase remains
maintainable and each domain can evolve independently.

The core constraint was: the architecture must be clean and domain-separated
without introducing operational overhead that a single developer cannot manage
within the capstone timeline.

---

## Decision

TalentBridge will be built as a **Modular Monolith**.

All five business domains — Identity, Jobs, Applications, Companies, and
Notifications — are deployed as a single ASP.NET Core process in one Azure
Web App. However, each domain is a fully isolated module with its own folder
structure, its own DbContext, its own MediatR command and query handlers, and
no direct code references to other module internals.

The physical structure enforces this:

```
src/
  API/
    TalentBridge.API/              ← single entry point, single deployment unit
  Modules/
    Identity/
      TalentBridge.Identity.Domain/
      TalentBridge.Identity.Application/
      TalentBridge.Identity.Infrastructure/
    Jobs/
      TalentBridge.Jobs.Domain/
      TalentBridge.Jobs.Application/
      TalentBridge.Jobs.Infrastructure/
    Applications/
      TalentBridge.Applications.Domain/
      TalentBridge.Applications.Application/
      TalentBridge.Applications.Infrastructure/
    Companies/
      ...
    Notifications/
      ...
  Shared/
    TalentBridge.Shared/           ← only shared primitives, no domain logic
```

Cross-module communication happens through domain events published via the
Outbox pattern to Azure Service Bus — not through direct method calls between
modules. This ensures modules remain decoupled even though they share a process.

---

## Alternatives Considered

### Alternative 1 — Microservices

Each domain (Identity, Jobs, Applications, Companies, Notifications) would be a
separate deployable service with its own container, its own database, its own
CI/CD pipeline, and its own API gateway entry.

**Why rejected:**

Microservices require significant infrastructure before a single feature works.
Each service needs its own container registry entry, deployment pipeline, health
checks, service discovery, distributed tracing wiring, and inter-service
authentication. With one developer and a six-day timeline, the majority of time
would be spent on infrastructure plumbing rather than building the actual hiring
platform features. The student subscription also has Azure Container Apps quota
limits that would restrict running five separate services simultaneously.
Additionally, distributed transactions across service boundaries (for example,
creating a job and immediately publishing an application-available notification)
require either the Saga pattern or two-phase commit, both of which add weeks of
complexity. The operational cost — debugging, log correlation across five
services, network latency between services — is not justified for a team of one.

### Alternative 2 — Single Layer Monolith (Big Ball of Mud)

All code in one project with no module boundaries. Controllers call repositories
directly, no domain layer, no separation between business domains.

**Why rejected:**

A single-layer monolith creates tight coupling immediately. A change in how Jobs
are stored would require understanding how Applications references that data.
Authentication logic would bleed into Jobs logic. The codebase becomes
impossible to reason about as features grow. This approach also provides no
migration path — if TalentBridge needed to extract a module into its own service
later, there would be no clean boundary to cut along.

### Alternative 3 — Vertical Slice Architecture

No horizontal layers (Domain/Application/Infrastructure). Instead, each feature
is a single vertical slice — all the code for "Post a Job" lives in one folder
regardless of layer.

**Why rejected:**

Vertical slices work well for simple CRUD applications but make it harder to
enforce domain invariants across features within the same domain. For TalentBridge,
the Jobs domain has rules that span multiple features (a job can only be closed
by the HR user who posted it, a job must have at least one open slot before
applications are accepted). These rules are best expressed in a domain model with
aggregate roots and domain events, which the Domain/Application/Infrastructure
layering supports naturally.

---

## Consequences

### Positive

**Simple deployment.** One `dotnet publish`, one container image, one Azure Web
App deployment. A single CI/CD pipeline handles the entire system. No service
mesh, no API gateway, no inter-service network configuration.

**Shared transaction boundary.** Within a single request, multiple domain
operations can participate in one database transaction. For example, creating a
job application and publishing the ApplicationSubmitted domain event to the
outbox table happen atomically — if either fails, both roll back.

**Easy local development.** One `dotnet run` command starts the entire system.
No Docker Compose with five services, no port mapping, no service discovery
required locally.

**Clean migration path.** Because each module has strict boundaries (its own
DbContext, no direct internal references from other modules), any single module
can be extracted into its own microservice in the future by moving its three
projects out and adding a network transport layer. The domain events over
Service Bus already provide the decoupling needed for this.

**Straightforward observability.** One Application Insights resource, one
connection string, one distributed trace per request. No cross-service trace
correlation required.

### Negative

**Shared failure domain.** If one module has a bug that crashes the process
(for example, a Notifications module infinite loop), all modules go down. In a
microservices architecture, only the Notifications service would fail.

**Cannot scale modules independently.** If the Jobs module receives ten times
more traffic than Identity, the entire monolith must be scaled horizontally.
There is no way to add more instances of just the Jobs module.

**Shared database server.** All five modules connect to the same Azure SQL
server (each with their own schema/DbContext but same server). A long-running
query in one module can affect connection pool availability for others.

**Deployment coupling.** A bug fix in the Notifications module requires
redeploying the entire application, including Identity and Jobs, even though
they are unchanged.

---

## Confirmation This Was the Right Choice

After six days of development, the modular monolith delivered:

- Five fully functional business domains with clean separation
- One Azure Web App deployment with a single CI/CD pipeline
- Domain events flowing between modules via Azure Service Bus outbox
- OpenTelemetry traces showing the full request path across module boundaries
- A security pass (STRIDE, ZAP, private endpoints) completed in one day
  without touching deployment infrastructure

A microservices approach with the same feature set would have required
significantly more time on infrastructure alone, leaving less time for the
actual business logic, observability, and security work that was completed.

---

## Related Decisions

- ADR-0002 (planned) — Use Outbox Pattern for domain event publishing
- ADR-0003 (planned) — Use JWT Bearer over session-based authentication
