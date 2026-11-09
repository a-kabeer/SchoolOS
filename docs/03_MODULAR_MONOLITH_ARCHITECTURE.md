# 03 — Technical Architecture: ASP.NET Core Modular Monolith

> **Hybrid deployment update (v1.1):** This document's Modular Monolith guidance applies to the **Local Data Plane business application**. The deployment now has two planes: (1) SchoolOS Cloud Control Plane for subscriptions, licenses, releases and sync coordination, and (2) one or more Local Data Plane nodes for school operations. This does not mean turning every business module into a microservice. See `14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md` for the authoritative topology.

## 1. Architecture choice

Use a **Domain-Oriented Modular Monolith**.

A modular monolith is one deployable backend application organized into strongly bounded business modules. It is not one large unstructured codebase and it is not a set of independently deployed microservices.

### Why this fits the project

- The team has ASP.NET Core and React experience.
- Financial workflows need strong transactional consistency.
- A small team should avoid the operational burden of many services.
- Module boundaries allow independent reasoning and testing.
- A module can be extracted later if scale or team ownership justifies it.

## 2. Logical system diagram

```text
Users / Browser / Installed PWA
              |
              v
React + TypeScript Frontend
              |
         HTTPS / API
              |
              v
Local ASP.NET Core API Host (one deployment per Local Node)
              |
   +----------+-----------+----------------+
   |                      |                |
Identity & Scope      Module Endpoints   Background Workers
   |                      |                |
   +----------------------+----------------+
                          |
                    Module Layer
                          |
     +-----------+--------+---------+-------------+
     |           |        |         |             |
  Students     Fees    Finance  Attendance    Exams ...
     |           |        |         |             |
     +-----------+--------+---------+-------------+
                          |
              Module Contracts / Events
                          |
                Infrastructure Layer
                          |
      PostgreSQL + File Storage + External APIs
                          |
       AI / SMS / WhatsApp / Email / Payment Providers
```

## 3. Backend technology baseline

- ASP.NET Core on .NET 10 LTS
- C# with nullable reference types enabled
- Entity Framework Core 10
- Npgsql EF Core provider compatible with EF Core 10
- PostgreSQL
- ASP.NET Core Identity or a selected identity system
- Policy-based authorization
- OpenAPI/Swagger for API documentation
- BackgroundService for initial background workers
- Transactional Outbox for reliable event dispatch
- Structured logging and distributed tracing (OpenTelemetry recommended)
- Docker for repeatable development and deployment

Verify supported package patches and hosting requirements when implementation starts.

## 4. Frontend technology baseline

- React + TypeScript
- Vite build tooling (recommended)
- React Router
- TanStack Query for server state
- React Hook Form + Zod for forms and client validation
- TanStack Table for data-heavy grids
- IndexedDB through Dexie or equivalent
- Service Worker / PWA manifest
- Workbox or another maintained service-worker tool
- Shared design system and RTL support

## 5. Solution layout (suggested)

```text
SchoolOS/
  SchoolOS.sln
  src/
    Hosts/
      SchoolOS.Cloud.Api/       # Control Plane host
      SchoolOS.Local.Api/       # Local Data Plane host
    PlatformModules/            # Cloud-only platform domains
      Tenants/
      Subscriptions/
      Licensing/
      NodeRegistry/
      SyncHub/
      ReleaseManagement/
    BuildingBlocks/
      SharedKernel/
      ModuleAbstractions/
      Security/
      Observability/
    Modules/
      Identity/
      Organization/
      Admissions/
      Students/
      Fees/
      Finance/
      Reconciliation/
      Academics/
      Attendance/
      Examinations/
      HR/
      Communication/
      AI/
      Reporting/
      Audit/
    Infrastructure/
      Persistence/
      FileStorage/
      Messaging/
  frontend/
    schoolos-web/
  tests/
    UnitTests/
    IntegrationTests/
    ArchitectureTests/
    SecurityTests/
    E2ETests/
  docs/
  deploy/
```

A mature module may be split into projects such as:

- `SchoolOS.Modules.Fees.Domain`
- `SchoolOS.Modules.Fees.Application`
- `SchoolOS.Modules.Fees.Infrastructure`
- `SchoolOS.Modules.Fees.Contracts`

Do not create a large number of projects merely for appearance. The architecture tests and dependency rules matter more than folder count. Start with a manageable project structure and split modules where boundaries and team needs justify it.

## 6. Module boundaries and dependency rules

1. Each module owns its domain rules and write model.
2. A module does not directly write another module's tables.
3. Cross-module actions use public application contracts or events.
4. No circular project references.
5. SharedKernel contains only genuinely shared primitives; it must not become a dumping ground.
6. Reporting uses controlled read models/projections rather than unrestricted table access.
7. External provider SDKs belong behind adapters in Infrastructure.
8. Domain entities should not depend on ASP.NET controllers or external AI provider SDKs.

## 7. Synchronous and asynchronous communication

### Synchronous

Use a module application interface when an immediate answer or validation is required. Example: Admissions requests an enrollment eligibility check from Students/Organization.

### Asynchronous

Use domain/integration events for follow-up work. Example:

`FeePaymentRecorded → Finance posting request / Notification request / Reporting projection update`

Notifications and analytics must not make a successfully committed payment fail merely because an external provider is temporarily unavailable.

### Transactional Outbox

Write the business change and its outbox message in the same database transaction. A background worker dispatches pending messages, records attempts and handles retries. Consumers must be idempotent.

## 8. Persistence approach

- Each Local Data Plane has its own operational PostgreSQL database.
- The Cloud Control Plane has a separate platform database; it does not directly query or update local operational tables.
- Separate logical schema per module where appropriate.
- Module-owned EF Core DbContext/migrations or a controlled migration strategy.
- Avoid unrestricted shared DbContext access across modules.
- Use database transactions for financial operations.
- Use unique constraints and idempotency keys for retried requests.
- Use optimistic concurrency for editable records where appropriate.

## 9. API design

- Version APIs, e.g. `/api/v1/...`.
- Use DTOs rather than exposing EF entities.
- Standardize error responses and validation.
- Apply authorization at endpoint and application-use-case levels.
- Derive tenant and scope from authenticated server-side context, not arbitrary client input.
- Use pagination, filtering and sorting for large lists.
- Use asynchronous job-status endpoints for lengthy AI/report operations.
- Document endpoints with OpenAPI.

## 10. Deployment shape

Hybrid production has two deployment environments:

**Cloud Control Plane:**
- SchoolOS-hosted ASP.NET Core platform host.
- Separate platform PostgreSQL database.
- Subscription, license, node registry, release and sync modules.
- Protected signing-key storage and monitoring.

**Each Local Data Plane:**
- React static assets served from the Local Node (no CDN dependency for local operation).
- ASP.NET Core Local API / school-business Modular Monolith.
- Local PostgreSQL database.
- Local background/sync workers from the same codebase.
- Private document storage and persistent volumes.
- Optional Redis only when a demonstrated local requirement exists.

The Cloud and Local hosts are separate deployment/trust boundaries. The business modules remain a Modular Monolith inside the Local host; the product does not split every school module into a microservice.

## 11. Testing architecture

- Unit tests for domain rules.
- Integration tests for module persistence and transactions.
- Architecture tests for prohibited dependencies and circular references.
- Security tests for tenant/campus boundaries.
- Contract tests for public module interfaces and event payloads.
- End-to-end tests for full school workflows.
- Offline tests for retries, duplicate uploads, conflict and force-close scenarios.


## 12. Hybrid deployment implications

- The local school application is the operational authority for records owned by its site/node.
- Cloud Control Plane has a separate database and a separate trust boundary.
- Local business modules must not depend on a live call to Cloud Control Plane for ordinary local operations.
- Subscription and license state is represented locally by a signed entitlement snapshot/lease.
- Synchronization is handled by dedicated Outbox/Inbox workers and a Cloud Sync Hub; it is not a direct cross-database write.
- The same modular monolith package can be installed on several campus nodes. Site identity and data ownership are configuration, not a separate code fork.
- See docs 14–17 and ADR-011 through ADR-013.


## 13. Two-host, one business modular-monolith model

The repository may contain two ASP.NET Core hosts:

- `SchoolOS.Cloud.Api`: a small platform-oriented Modular Monolith for tenant registry, subscription/billing, license issuance, node registry, sync hub and signed releases.
- `SchoolOS.Local.Api`: the school-business Modular Monolith for admissions, students, finance, attendance, academics, examinations and local operations.

They share versioned contracts and selected building blocks, but each has its own database and trust boundary. Do not let the Cloud host directly connect to a customer's local operational database. The Sync Hub exchanges authenticated events and commands instead.
