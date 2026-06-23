# ADR-0004 — Use Azure Service Bus over Direct API Calls for Cross-Module Communication

**Status:** Accepted  
**Date:** 2026-06-22  
**Author:** amey2612  
**Project:** TalentBridge — Enterprise Hiring Platform  

---

## Context

TalentBridge has five business modules — Identity, Jobs, Applications, Companies,
and Notifications — running as a single ASP.NET Core process (see ADR-0001). Each
module owns its own DbContext and its own command/query handlers. Modules must
remain logically decoupled: no module should import or directly call code from
another module's internals.

Several real business workflows cross module boundaries:

- When a candidate submits an application (`ApplicationSubmittedEvent`), the
  Notifications module must send a confirmation email.
- When an application is accepted (`ApplicationAcceptedEvent`), the Notifications
  module must alert the candidate.
- When a job is published (`JobPublishedEvent`), the Notifications module must
  send job-alert emails to matching candidates.
- When a company is approved (`CompanyApprovedEvent`), the Identity module (or a
  future module) may need to update access rights.

The question is: how should one module notify another that something important
has happened, without creating a direct code dependency between them?

The two broad options are:

1. **Direct call** — the Applications module calls the Notifications module's
   endpoint or service method directly at the time the event occurs.
2. **Message broker** — the Applications module publishes an event to a shared
   transport; the Notifications module subscribes and reacts independently.

TalentBridge already has Azure Service Bus provisioned as part of the Azure
infrastructure (`talentbridge-sb-amey.servicebus.windows.net`). The decision is
whether to use it as the cross-module communication layer or to wire modules
together with direct calls.

---

## Decision

TalentBridge will use **Azure Service Bus topics and subscriptions** for all
cross-module event-driven communication.

The flow is:

1. A domain operation completes and raises a domain event (e.g.
   `ApplicationSubmittedEvent`).
2. The event is written to the `OutboxMessages` table atomically with the
   business entity change (see ADR-0002).
3. The `OutboxRelayService` picks up the pending outbox row and publishes a
   `ServiceBusMessage` to the topic `talentbridge-events`, with:
   - `MessageId` set to the outbox row's `Id` (for deduplication)
   - `Subject` set to the event type name (e.g. `"ApplicationSubmitted"`)
   - `Body` containing the JSON-serialized event payload
4. The `TalentBridgeEventConsumer` in the Notifications module listens on the
   `notifications` subscription of the same topic. On receiving a message, it
   routes by `Subject` and dispatches the appropriate notification action.
5. Before processing, it checks `IProcessedMessageStore.IsProcessedAsync` to
   skip already-handled messages (idempotency guard). After successful
   processing, it calls `CompleteMessageAsync`. On failure it calls
   `AbandonMessageAsync`, allowing Service Bus to retry with its built-in
   dead-letter policy.

```
Applications Module          Service Bus Topic             Notifications Module
─────────────────            ──────────────────            ─────────────────────
ApplicationSubmitted   →→→   talentbridge-events   →→→    TalentBridgeEventConsumer
  (via OutboxRelay)          [notifications sub]           (BackgroundService)
```

The consumer is configured with:
- `MaxConcurrentCalls = 5` — up to five messages processed in parallel
- `AutoCompleteMessages = false` — explicit complete/abandon control so a
  processing failure does not silently discard the message

No module has a project reference or method call into another module. The
Applications module has no knowledge of the Notifications module. The
Notifications module has no knowledge of the Applications module. Their only
shared contract is the event type name string used as the `Subject`.

---

## Alternatives Considered

### Alternative 1 — Direct HTTP Call from Publisher to Consumer

When the Applications command handler finishes saving an application, it makes
an HTTP call to `POST /api/notifications/application-submitted` (or calls the
Notifications module's service interface directly via DI).

**Why rejected:**

Direct calls — whether HTTP or in-process method calls — create tight coupling.
If the Notifications module is slow or throws an exception, it blocks the
Applications command handler. The user's "Submit Application" request now has a
latency dependency on the Notifications module. If notifications are temporarily
down, the entire application submission fails. This violates the module
isolation principle described in ADR-0001: a bug or outage in Notifications
should never prevent a candidate from submitting an application.

With HTTP calls specifically, the Applications module must know the Notifications
module's endpoint URL, which must be kept in sync as the API evolves. With
direct DI calls, the Applications module's project must reference the
Notifications module's project, creating a compile-time dependency — exactly
what the modular monolith architecture is designed to prevent.

### Alternative 2 — MediatR In-Process Notifications Only (`INotification`)

Use MediatR's `INotification` / `INotificationHandler<T>` to dispatch domain
events in-process. The Applications module publishes an `INotification` via
`IPublisher.Publish()`; the Notifications module registers a handler that
receives it.

**Why rejected:**

MediatR `INotification` dispatch happens synchronously in the same request
pipeline. If a notification handler throws, it can bubble up and fail the
original command. More importantly, in-process notifications are fire-and-forget
within the same process lifetime — if the process restarts between the command
completing and the notification dispatching, the event is lost. There is no
durability. MediatR notifications also cannot be consumed by any component
outside the current process, which removes the possibility of extracting
Notifications into its own service later. The Outbox + Service Bus approach
(ADR-0002 combined with this decision) provides both durability and the same
module decoupling that MediatR offers, at the cost of slightly more latency.

### Alternative 3 — Azure Event Grid

Use Azure Event Grid as the message transport instead of Azure Service Bus.
Publishers post events to an Event Grid topic; subscribers receive them via
webhook or Event Grid subscription.

**Why rejected:**

Azure Event Grid is optimized for reactive, infrastructure-level events (blob
created, VM started, resource group changed) and fan-out to many subscribers
via HTTP webhooks. For application-level domain events between modules, Service
Bus topics offer better semantics: message sessions, dead-lettering, explicit
complete/abandon, peek-lock for reliable processing, and durable subscription
state. Event Grid has a 24-hour delivery retry window and assumes the subscriber
endpoint is an HTTPS URL, which adds complexity when the consumer is a
BackgroundService inside the same process. Service Bus topics give explicit
control over retry counts, dead-letter thresholds, and lock duration without
requiring an externally accessible webhook endpoint.

### Alternative 4 — Azure Storage Queue

Use Azure Storage Queues as the message transport. Publishers enqueue messages;
consumers poll and dequeue.

**Why rejected:**

Azure Storage Queues are a point-to-point queue (one consumer per message).
Service Bus topics support multiple independent subscriptions on the same
message, meaning a second future subscriber (e.g., an Analytics module or an
Audit module) can subscribe to `talentbridge-events` without the publisher
changing anything. Storage Queues would require either a separate queue per
consumer (publishers must know every consumer) or a fan-out relay that reads
from one queue and writes to many — which adds the same operational complexity
as Service Bus, without its built-in topic/subscription model.

---

## Consequences

### Positive

**Complete module isolation.** The Applications module has no project reference,
no interface dependency, and no compile-time knowledge of the Notifications
module. The only contract between producer and consumer is the `Subject` string
(`"ApplicationSubmitted"`, `"JobPublished"`, etc.). A new subscriber can be
added — or an existing subscriber changed entirely — without touching the
publishing module.

**Publisher failure isolation.** If the Notifications consumer throws an
exception while handling `ApplicationSubmitted`, it calls `AbandonMessageAsync`
and the message returns to the subscription queue for retry. The original
application submission that the candidate made was already committed to the
database and is unaffected.

**Durable delivery.** Because messages flow through the outbox (ADR-0002) before
reaching Service Bus, and Service Bus stores messages durably until a subscriber
explicitly completes them, events survive both application restarts and transient
Service Bus connectivity issues.

**Future-proof for extraction.** If the Notifications module is ever extracted
into its own deployed service, the only change required is pointing its
`ServiceBusClient` at the same namespace with its own Managed Identity. The
topic name, subscription name, and message contract remain identical. No code
changes in the publishing modules.

**Multiple independent subscribers.** A future Analytics module can add a second
subscription (`analytics`) to the `talentbridge-events` topic and receive every
event independently of the `notifications` subscription, without any change to
the publishers or to the Notifications module.

### Negative

**Eventual consistency.** The Notifications module does not react instantly. A
message travels through the outbox relay (up to 5 seconds poll interval, from
ADR-0002), then through Service Bus delivery. The total latency from domain
event to notification action is typically under 10 seconds in normal operation,
but it is not synchronous. This is acceptable for email notifications but would
not be acceptable for use cases requiring real-time UI updates (which would need
WebSockets or Server-Sent Events instead).

**Idempotency requirement on consumers.** Service Bus guarantees at-least-once
delivery. The `TalentBridgeEventConsumer` uses `IProcessedMessageStore` to skip
already-processed messages. The current implementation of `IProcessedMessageStore`
is in-memory (`InMemoryProcessedMessageStore`), meaning it resets on process
restart and does not protect against duplicates across instances. A persistent
store (Redis or a database table) is required before multi-instance deployment.

**Azure resource dependency.** Cross-module communication now depends on Azure
Service Bus being reachable. Local development requires either a live Service Bus
namespace or the Azure Service Bus emulator. Running integration tests without
a Service Bus namespace available requires mocking `ServiceBusClient`, which
adds test complexity.

**Dead-lettered messages need monitoring.** Messages that fail all Service Bus
retries land in the dead-letter queue. Without an alert on dead-letter queue
depth, silent message loss is possible. An Azure Monitor alert on
`talentbridge-events/notifications` dead-letter count should be added before
production use.

---

## Related Decisions

- ADR-0001 (accepted) — Use Modular Monolith instead of Microservices
- ADR-0002 (accepted) — Use Outbox Pattern for domain event publishing
- ADR-0003 (accepted) — Use JWT Bearer over session-based authentication
