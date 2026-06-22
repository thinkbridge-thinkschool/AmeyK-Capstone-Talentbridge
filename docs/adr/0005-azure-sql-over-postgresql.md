# ADR-0005 — Use Azure SQL (SQL Server) over PostgreSQL

**Status:** Accepted  
**Date:** 2026-06-22  
**Author:** amey2612  
**Project:** TalentBridge — Enterprise Hiring Platform  

---

## Context

TalentBridge requires a relational database to persist data for five business
modules: Identity (users), Jobs (job postings), Applications (candidate
applications), Companies (company profiles), and Notifications (outbox relay
state). Each module has its own EF Core DbContext backed by the same database
server, with module-scoped tables and separate EF Core migration histories.

The application stack is ASP.NET Core 10 on .NET 10, deployed to Azure App
Service with Azure infrastructure managed via Bicep. The data access layer uses
Entity Framework Core with migrations generated at development time and applied
at deployment time.

Two relational database platforms were viable for Azure deployment:

- **Azure SQL** — Microsoft's fully managed SQL Server PaaS on Azure
- **Azure Database for PostgreSQL Flexible Server** — the open-source PostgreSQL
  engine hosted as a managed Azure service

The choice has consequences for Managed Identity integration, EF Core provider
maturity, private network connectivity, monitoring tooling, and the connection
string format used across all five modules.

The project uses a student Azure subscription with limited quota. The database
server is `talentbridge-sql-amey.database.windows.net`, provisioned in the
Bicep module at `infra/modules/sql.bicep`.

---

## Decision

TalentBridge will use **Azure SQL (SQL Server)** with EF Core's
`Microsoft.EntityFrameworkCore.SqlServer` provider.

The concrete configuration across all five modules uses:

```
Server=tcp:talentbridge-sql-amey.database.windows.net,1433;
Initial Catalog=talentbridge-sql-amey-db;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

`Authentication=Active Directory Default` means the application never stores
a database password. Locally, `az login` provides the credential. In Azure,
the App Service Managed Identity is used automatically. No password appears in
`appsettings.json`, environment variables, or Key Vault secrets for the
connection string itself.

The server is provisioned with:
- `minimalTlsVersion: '1.2'` — TLS 1.0 and 1.1 rejected at the server level
- `publicNetworkAccess: 'Disabled'` — the public endpoint is switched off;
  all connections must come through the private endpoint
- A VNet private endpoint bound to the `private-endpoints-subnet`
  (10.0.2.0/24) with a private DNS zone `privatelink.database.windows.net`

The database SKU is Basic (5 DTUs, 2 GB) — the lowest Azure SQL tier,
sufficient for the capstone workload.

Each module's DbContext is registered with `UseSqlServer()`:

```csharp
services.AddDbContext<IdentityDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("TalentBridgeDb")));
```

All five modules share the same SQL Server but each module's tables are
namespaced by convention (e.g. `JobsOutboxMessages`,
`ApplicationsOutboxMessages`). EF Core migrations per module are generated and
applied independently.

---

## Alternatives Considered

### Alternative 1 — Azure Database for PostgreSQL Flexible Server

PostgreSQL hosted on Azure as a managed service, using EF Core's
`Npgsql.EntityFrameworkCore.PostgreSQL` provider.

**Why rejected:**

**Managed Identity connection string.** Azure Database for PostgreSQL Flexible
Server supports Azure AD authentication, but the EF Core Npgsql provider
requires a custom `NpgsqlDataSource` or a token provider plugin
(`Azure.Identity` + `NpgsqlAuthenticationPlugin`) to pass the Azure AD access
token at runtime. This is a non-trivial setup that requires additional NuGet
packages and configuration compared to Azure SQL's single connection string
keyword `Authentication=Active Directory Default`. For a project where every
module shares the same connection string pattern, reducing the authentication
setup to one keyword reduces risk of misconfiguration across five DbContext
registrations.

**Private endpoint DNS zone.** Azure SQL uses the well-known private DNS zone
`privatelink.database.windows.net`. Azure Database for PostgreSQL uses
`privatelink.postgres.database.azure.com`. While Bicep supports both, the
security pass (Day 26) was built around the SQL private endpoint; switching
would require changing the VNet module, the DNS zone configuration, and
retesting the private endpoint connectivity.

**EF Core provider maturity on .NET 10.** The `SqlServer` EF Core provider is
maintained by Microsoft and ships on the same release cycle as .NET. The Npgsql
provider is community-maintained and excellent, but version alignment with
EF Core 10 and .NET 10 must be verified at project start. On a six-day
capstone timeline, a version mismatch between EF Core and the Npgsql provider
introduces risk that has no upside for a Microsoft-stack project.

**Migration regeneration cost.** All five modules already have `InitialCreate`
migrations written against SQL Server (T-SQL DDL). Switching to PostgreSQL would
require dropping all existing migrations and regenerating them against a
PostgreSQL database, then verifying that all column type mappings (particularly
`DECIMAL(18,2)` for salary columns, `NVARCHAR(MAX)` for payload columns, enum
`HasConversion<string>()`) produce the correct PostgreSQL equivalents. This is
mechanical but time-consuming for five module migration histories.

**Tooling alignment.** The project was developed on Windows with Azure Data
Studio and SSMS available for query inspection and schema verification.
SQL Server Management Studio and Azure Data Studio's SQL Server extension have
first-class support for Azure SQL, including query performance insight
integration and direct connection from the developer machine through Azure
Active Directory authentication.

### Alternative 2 — SQLite (Local Development Only)

Use SQLite for all environments, including production.

**Why rejected:**

SQLite does not support multiple concurrent writers without serializing all
writes. For a web API with five modules, each capable of receiving concurrent
requests, SQLite's single-writer model would create a bottleneck and
intermittent `database is locked` errors. SQLite also lacks the features that
TalentBridge relies on: precise decimal types for salary columns, row-level
security, connection pooling, and Managed Identity authentication. SQLite is
appropriate only as a test database for unit tests that do not test SQL
Server-specific behavior.

### Alternative 3 — Azure Cosmos DB (NoSQL)

Use Cosmos DB as the primary datastore, modeling each module's aggregate as a
document.

**Why rejected:**

TalentBridge's domain model is fundamentally relational. A `JobApplication`
references a `Job` (by `JobId`) and a `User` (by `CandidateId`). Queries like
"list all open applications for a given job" are natural SQL joins. In Cosmos DB
these require either denormalizing the data (duplicating job data into every
application document) or executing multiple round-trip queries and joining in
application code. The Outbox Pattern (ADR-0002) also relies on writing the
domain event and the business entity in a single atomic SQL transaction; Cosmos
DB's transactional batch is limited to items within the same logical partition,
making cross-entity atomic writes more constrained. EF Core's Cosmos provider
exists but is less mature than the SQL Server provider and does not support all
LINQ query patterns used in the codebase. Cosmos DB is the right choice for
globally distributed, schema-flexible, high-throughput document workloads —
not for a five-domain relational hiring platform with referential integrity
requirements.

---

## Consequences

### Positive

**Passwordless authentication everywhere.** `Authentication=Active Directory
Default` in the connection string is the only authentication configuration
required. No secrets to rotate, no passwords in Key Vault, no risk of a leaked
database credential in logs. The same connection string works for a developer
using `az login` locally and for the App Service using its Managed Identity in
Azure.

**Private endpoint with standard DNS.** The `privatelink.database.windows.net`
DNS zone integrates cleanly with Azure's private DNS infrastructure. The Bicep
module provisions the private endpoint, VNet link, and DNS zone group as a
unit. No additional network configuration is required for traffic from the App
Service to the database to stay entirely within the Azure VNet.

**EF Core migrations are straightforward.** All five module DbContexts use
identical `UseSqlServer()` registration and share one connection string. Adding
a new column requires `dotnet ef migrations add` in the relevant module project
— no cross-provider type mapping concerns.

**Decimal precision native to SQL Server.** Salary columns use
`HasPrecision(18, 2)` which maps directly to `DECIMAL(18,2)` in SQL Server.
Financial figures are stored and compared without floating-point rounding errors.

**Azure Monitor SQL Insights.** Azure SQL integrates with Azure Monitor and
Log Analytics for query performance monitoring, slow query detection, and
automated tuning recommendations — the same Log Analytics workspace already
used by Application Insights for the API traces.

**Consistent Microsoft stack.** The entire deployment stack — ASP.NET Core,
Azure App Service, Azure Key Vault, Azure Service Bus, Azure SQL — uses
Microsoft's managed services with consistent Managed Identity authentication
and consistent Bicep resource provider APIs. There are no cross-vendor
authentication models to reconcile.

### Negative

**Vendor lock-in.** Azure SQL is a Microsoft-proprietary PaaS service. The
`UseSqlServer()` EF Core provider and the T-SQL dialect in raw queries are
not portable to other cloud providers without migration effort. Switching to
PostgreSQL on AWS or GCP would require regenerating all five module migrations
and testing all queries against the new engine. The EF Core abstraction reduces
but does not eliminate this concern.

**Basic SKU limitations.** The Basic tier (5 DTUs) does not support In-Memory
OLTP, columnstore indexes, or read replicas. If TalentBridge were to grow to
production scale and required read-heavy analytics queries on job applications,
upgrading to Standard or Premium (or migrating to Hyperscale) would be required.
The DTU-based Basic tier is also being gradually superseded by the vCore-based
serverless tier, which would be a better choice for production.

**Shared server across modules.** All five modules connect to the same Azure
SQL server. A long-running query or a heavy migration in one module's DbContext
can reduce the available DTUs for other modules. Module-level isolation at the
database server level would require five separate database instances, which
would exceed the student subscription quota and significantly increase cost.

**Private endpoint requires VNet.** The `publicNetworkAccess: 'Disabled'`
setting means local development cannot connect to the production database
directly. Developers must either use a VPN/Bastion host, a local SQL Server
instance, or a separate development Azure SQL instance with public access
enabled. For the capstone, local development uses a separate connection string
pointing to a local SQL Server Express instance.

---

## Related Decisions

- ADR-0001 (accepted) — Use Modular Monolith instead of Microservices
- ADR-0002 (accepted) — Use Outbox Pattern for domain event publishing
- ADR-0003 (accepted) — Use JWT Bearer over session-based authentication
- ADR-0004 (accepted) — Use Azure Service Bus over direct API calls
