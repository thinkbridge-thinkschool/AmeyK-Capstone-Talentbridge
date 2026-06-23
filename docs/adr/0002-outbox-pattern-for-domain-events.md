# ADR-0002 — Use Outbox Pattern for Domain Event Publishing

**Status:** Accepted  
**Date:** 2026-06-22  
**Author:** amey2612  
**Project:** TalentBridge — Enterprise Hiring Platform  

---

## Context

TalentBridge uses domain events to communicate state changes between its five
modules. When a candidate submits an application, the Applications module raises
an `ApplicationSubmittedEvent`. When a job is published, the Jobs module raises
a `JobPublishedEvent`. The Notifications module (and any future subscriber) must
react to these events to send emails, update counters, or trigger other workflows.

The transport for cross-module events is Azure Service Bus. The problem is that
publishing directly to Service Bus inside a business operation creates a
distributed transaction problem: the database write and the Service Bus publish
are two separate I/O operations, and they can diverge.

Consider the failure modes with naive direct publishing:

**Scenario A — publish succeeds, database write fails.**  
The event fires on the bus. The business operation rolls back. Subscribers
receive an event for a state change that never actually happened.

**Scenario B — database write succeeds, publish fails.**  
The application is in its new state. No subscriber is notified. The
`ApplicationSubmittedEvent` is silently dropped. The candidate receives no
confirmation email.

Both scenarios produce silent, hard-to-debug inconsistencies. There is no
built-in two-phase commit between SQL Server and Azure Service Bus that would
make both writes atomic.

The domain events that must be published reliably are:

- `ApplicationSubmittedEvent`, `ApplicationStatusChangedEvent`,
  `ApplicationAcceptedEvent`, `ApplicationWithdrawnEvent` (Applications module)
- `JobCreatedEvent`, `JobPublishedEvent`, `JobClosedEvent` (Jobs module)
- `UserRegisteredEvent` (Identity module)
- `CompanyCreatedEvent`, `CompanyApprovedEvent` (Companies module)

The system must guarantee that every domain event that corresponds to a
committed database state change eventually reaches Azure Service Bus, even if
the process crashes between the database write and the publish.

---

## Decision

TalentBridge will use the **Outbox Pattern** for all domain event publishing.

When a domain operation completes, the handler writes the domain event as a row
in the `OutboxMessages` table inside the **same database transaction** as the
business data change. This is a local write — no network call, no Service Bus
involved. The business transaction commits atomically: either both the business
entity change and the outbox row land in the database, or neither does.

A separate background service (`OutboxRelayService`, a .NET `BackgroundService`)
polls the `OutboxMessages` table every five seconds. For each row where
`ProcessedOnUtc IS NULL`, it serializes the payload and sends it to the Azure
Service Bus topic `talentbridge-events`. After a successful send, it stamps
`ProcessedOnUtc = DateTime.UtcNow` on the row, marking it as delivered.

The shared outbox message structure in `TalentBridge.Shared.Outbox.OutboxMessage`:

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }          // used as Service Bus MessageId (dedup key)
    public string Type { get; set; }      // event class name, e.g. "ApplicationSubmittedEvent"
    public string Payload { get; set; }   // JSON-serialized event body
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }  // null = pending, non-null = delivered
}
```

The relay reads up to 50 pending messages per poll cycle (oldest first), sends
each to Service Bus, and marks each delivered immediately after its send
succeeds. If the process crashes after a send but before the `ProcessedOnUtc`
stamp is written, the message will be sent again on the next poll cycle. This
means delivery is **at-least-once** — consumers must be idempotent. The
`MessageId` on the Service Bus message is set to `OutboxMessage.Id` so Service
Bus duplicate detection can suppress exact duplicates within its detection window.

The relay runs inside the same ASP.NET Core process as the API, registered as a
hosted service. It is the only component that reads from the outbox and writes
to Service Bus.

---

## Alternatives Considered

### Alternative 1 — Direct Service Bus Publish Inside the Command Handler

The command handler publishes to Service Bus immediately after saving the
business entity, within the same `try/catch` block.

**Why rejected:**

This is the naive approach and it introduces the two failure scenarios described
in the Context section. There is no atomic guarantee between a SQL Server commit
and a Service Bus send. Scenario B (database saves, Service Bus send fails) is
particularly dangerous — it produces silent data loss that is difficult to
detect. Even with retry logic on the Service Bus client, a network partition or
Service Bus throttle could still cause the send to fail after all retries
exhaust, permanently losing the event. The Outbox Pattern eliminates this class
of failure entirely.

### Alternative 2 — Event Sourcing

Instead of a state-change model (save entity, emit event), every state mutation
is stored as an immutable event in an event store. The current state is derived
by replaying all past events. Publishing to Service Bus becomes reading from the
event log.

**Why rejected:**

Event sourcing solves the durability problem correctly but introduces significant
complexity: event versioning, projection rebuilding, snapshot management, and
a fundamentally different read model strategy. For a six-day capstone with one
developer, implementing event sourcing across five domains would consume the
majority of development time. The Outbox Pattern provides the same at-least-once
delivery guarantee with a fraction of the implementation complexity. Event
sourcing remains a valid future architecture if TalentBridge grows to require
full audit history and time-travel queries.

### Alternative 3 — Change Data Capture (CDC) via SQL Server

SQL Server's change feed (or Azure SQL's CDC feature) monitors the transaction
log and streams row changes to a downstream processor, which then publishes to
Service Bus.

**Why rejected:**

CDC is powerful but adds an external component (e.g. Azure Event Hubs with
Debezium, or a SQL Server CDC polling agent) that must be deployed, monitored,
and operated separately. It also requires granting the CDC component access to
the database transaction log at a low level, which complicates the security
model. For TalentBridge's student subscription, the additional Azure resource
cost and operational complexity are not justified. The Outbox Pattern achieves
the same goal using infrastructure that already exists (the SQL database and the
Service Bus), with no new dependencies.

### Alternative 4 — MediatR In-Process Events Only (No Service Bus)

Use MediatR `INotification` to dispatch domain events in-process. No outbox,
no Service Bus, no background relay.

**Why rejected:**

In-process events cannot survive a process restart or crash. They also cannot
be consumed by any future service outside the current process boundary. Using
in-process MediatR events for cross-module communication would couple all
modules to the same process forever, removing the clean migration path described
in ADR-0001. The Outbox Pattern with Service Bus preserves the option to extract
modules into separate services later — a subscriber registered on the bus topic
does not need to know or care whether the publisher is a monolith or a
standalone service.

---

## Consequences

### Positive

**Atomicity.** The outbox row and the business entity change are written in the
same SQL transaction. It is impossible for the business state to change without
an outbox row being created, and impossible for an outbox row to exist for a
state change that was rolled back.

**At-least-once delivery.** If the process crashes between the Service Bus send
and the `ProcessedOnUtc` stamp, the relay will re-send on the next poll. Every
committed state change is guaranteed to eventually produce at least one Service
Bus message.

**No new infrastructure.** The outbox uses the existing SQL database — no
additional Azure resource is required for the queue itself. Service Bus was
already a project dependency.

**Decoupled modules.** The Applications module writes to the outbox table. The
Notifications module reads from Service Bus. Neither module has a direct code
reference to the other. This is the cross-module communication boundary
described in ADR-0001.

**Observable.** Pending outbox rows are visible in the database and can be
queried directly. The relay logs every send and every error with structured
log output to Application Insights. A query on `OutboxMessages WHERE
ProcessedOnUtc IS NULL AND OccurredOnUtc < DATEADD(minute, -5, GETUTCDATE())`
immediately reveals stuck messages.

### Negative

**At-least-once, not exactly-once.** Consumers must handle duplicate events
idempotently. For example, if `ApplicationSubmittedEvent` is delivered twice,
the Notifications module must not send two confirmation emails to the candidate.
This adds an idempotency requirement to every event consumer.

**Polling latency.** The relay polls every five seconds. A domain event will
typically reach Service Bus within five seconds of the transaction commit, but
there is no sub-second guarantee. For workflows that require near-real-time
notification (e.g., immediate Slack alerts), this latency may need to be reduced
or replaced with a push-based mechanism.

**Outbox table growth.** Processed rows are never deleted by the current relay
implementation. Over time, the `OutboxMessages` table will grow. A cleanup job
or retention policy (e.g., delete rows where `ProcessedOnUtc < 30 days ago`)
must be added before production use at scale.

**Single relay instance.** The `OutboxRelayService` runs as a single
`BackgroundService` inside the API process. If the API is scaled to multiple
instances, multiple relays will race to process the same outbox rows, causing
duplicate sends more frequently. A distributed lock or a `SKIP LOCKED` query
hint on the outbox poll would be required in a multi-instance deployment.

---

## Confirmation This Was the Right Choice

The `SimulateCrash` flag in `OutboxRelayService` was used during development to
prove at-least-once delivery: with `SimulateCrash = true`, the relay throws an
exception after sending to Service Bus but before stamping `ProcessedOnUtc`. On
the next poll cycle, the same message is picked up and sent again — confirming
the delivery guarantee holds across process-level failures.

All ten domain event types across the five modules flow through this mechanism
without any module having a direct dependency on another module's code.

---

## Related Decisions

- ADR-0001 (accepted) — Use Modular Monolith instead of Microservices
- ADR-0003 (planned) — Use JWT Bearer over session-based authentication
