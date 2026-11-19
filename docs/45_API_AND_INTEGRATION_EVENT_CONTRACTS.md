# 45 — API and Integration Event Contract Specification

- **Document ID:** DOC-45
- **Status:** Draft
- **Decision maturity:** Proposed API/event contracts; DTO and OpenAPI review required
- **Owner:** Technical lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
**Version:** v1.8 draft · **Status:** Proposed contract baseline; endpoint names and DTOs require review  
**API base:** /api/v1  
**Companion:** docs/03_MODULAR_MONOLITH_ARCHITECTURE.md, docs/05_OFFLINE_PWA_AND_SYNC.md, docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md, docs/37_PLATFORM_API_CATALOG.md

## 1. API boundaries

SchoolOS has two separately secured API audiences:

### 1.1 Local Data Plane API

Owns day-to-day school operations:
- Identity and authorized local session management.
- Organization, campuses and academic calendar.
- Admissions, students, guardians, households and enrollments.
- Fees, finance, bank reconciliation.
- Academics, attendance, examinations and results.
- Communication requests and delivery status.
- Local device registration, offline operations, sync outbox and campus data exchange.

The Local API must continue approved operations when internet access to the Cloud Control Plane is unavailable.

### 1.2 Cloud Control Plane API

Owns platform/commercial operations:
- Customer organization registration and verification.
- CRM, public CMS and demo session lifecycle.
- Subscription plans, customer invoices, payment verification and commercial billing.
- Local Node registration, entitlement and signed licence lease issuance.
- Release catalogue and node health summaries.
- Sync Hub inbox/outbox, cursors and approved reporting projections.
- Customer Portal, Super Admin Console and Support Center.

The Control Plane must not provide unrestricted CRUD access to Local Data Plane student, finance, marks or attendance tables. Cross-plane actions use signed leases, authenticated events/commands and allow-listed projections.

## 2. General HTTP conventions

| Concern | Contract |
|---|---|
| Base path | /api/v1 |
| Payload | JSON UTF-8 unless a file upload/download is explicitly documented |
| Time | UTC ISO-8601 timestamps; school-day values use date-only strings |
| IDs | UUID format recommended; final ID strategy requires approval |
| Naming | camelCase JSON properties; stable public enum codes |
| Pagination | Cursor-based for high-volume/event data; page/size only for bounded admin lists |
| Sorting | Explicit allow-listed sort keys; no arbitrary SQL/order expression |
| Filtering | Typed, documented query parameters |
| Concurrency | rowVersion or ETag/If-Match for mutable resources |
| Correlation | X-Correlation-ID accepted/generated and propagated |
| Idempotency | Idempotency-Key on retriable state-changing requests |
| Error format | RFC 9457 Problem Details-style payload with stable application error code |
| Versioning | Additive compatible fields where possible; breaking contract changes require a versioning plan |
| Content security | DTOs only; never expose EF entities, credentials or internal storage paths |

### 2.1 Standard success envelope

For ordinary resource endpoints, return the resource DTO directly or a documented collection wrapper. Do not mix incompatible envelope shapes across modules.

Illustrative list wrapper:

~~~json
{
  "items": [],
  "nextCursor": null,
  "hasMore": false
}
~~~

### 2.2 Standard error shape

~~~json
{
  "type": "https://schoolos.example/errors/validation",
  "title": "Request validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/students",
  "code": "STUDENT_VALIDATION_FAILED",
  "correlationId": "00000000-0000-0000-0000-000000000000",
  "errors": [
    {
      "field": "admissionNumber",
      "code": "REQUIRED",
      "message": "Admission number is required."
    }
  ]
}
~~~

The example hostname and IDs are illustrative, not a production endpoint or real record.

### 2.3 Stable error codes

| HTTP status | Error code examples | Meaning |
|---|---|---|
| 400 | VALIDATION_FAILED, INVALID_STATE_TRANSITION | Request content or state invalid |
| 401 | AUTHENTICATION_REQUIRED, SESSION_EXPIRED | No valid identity/session |
| 403 | SCOPE_FORBIDDEN, ACTION_NOT_PERMITTED | Authenticated but not authorized |
| 404 | RESOURCE_NOT_FOUND | Resource absent or not visible in caller scope |
| 409 | VERSION_CONFLICT, DUPLICATE_OPERATION, ALREADY_PROCESSED | State/idempotency conflict |
| 410 | DEMO_SESSION_EXPIRED, LICENSE_RESOURCE_EXPIRED | Resource intentionally expired |
| 413 | FILE_TOO_LARGE | Upload size exceeds policy |
| 415 | UNSUPPORTED_MEDIA_TYPE | File/payload type not allowed |
| 422 | BUSINESS_RULE_VIOLATION, ALLOCATION_EXCEEDS_BALANCE | Valid syntax but violates business invariant |
| 429 | RATE_LIMITED | Retry after policy interval |
| 503 | DEPENDENCY_UNAVAILABLE, LOCAL_NODE_NOT_READY | Required dependency temporarily unavailable |

Do not reveal another tenant's resource existence. A forbidden cross-tenant object may be returned as 404 according to the approved anti-enumeration policy.

## 3. Authentication and scope contract

- Local school user authentication must be available at the Local Node during WAN/cloud outages.
- Control Plane customer portal, Super Admin, public/demo and support identities have separate authentication audiences/session boundaries.
- API handlers derive tenant, school, campus, user and role scope from validated server-side identity/membership.
- Client-supplied tenantId, campusId, studentId or sessionId is a requested resource identifier, never authorization evidence.
- Every resource read, list, update, export, attachment, report and background job rechecks the caller's scope.
- High-impact operations require step-up/MFA where the security policy says so.
- Tokens, password hashes, MFA secrets, node private credentials, signing keys and provider secrets never appear in normal response DTOs.

## 4. Local Data Plane endpoint families

These are proposed route families. Final route names and DTOs must be generated in an OpenAPI document and reviewed.

### 4.1 Organization and academic setup

| Method | Proposed route | Purpose |
|---|---|---|
| GET | /organization/schools | List schools in authorized scope |
| GET/POST | /organization/campuses | List/create campus |
| GET/POST | /organization/academic-years | List/create academic year |
| GET/POST | /organization/terms | List/create term |
| GET/POST | /organization/grade-levels | Grade setup |
| GET/POST | /organization/sections | Section setup |

Mutations require organization permissions and audit. Closing an academic year must not delete historical data.

### 4.2 Admissions and students

| Method | Proposed route | Purpose |
|---|---|---|
| GET/POST | /admissions/inquiries | Search/create inquiries |
| GET/POST | /admissions/applications | Search/create applications |
| POST | /admissions/applications/{id}/submit | Submit |
| POST | /admissions/applications/{id}/review | Record review outcome |
| POST | /admissions/applications/{id}/decision | Approve/reject/waitlist |
| GET/POST | /students | Search/create student profile |
| GET/PATCH | /students/{id} | Read/update permitted profile fields |
| GET/POST | /students/{id}/guardians | Manage verified guardian links |
| GET/POST | /households | Search/create household |
| GET/POST | /students/{id}/enrollments | Read/create enrollment history |
| POST | /students/{id}/transfers | Start approved transfer |
| POST | /students/{id}/withdrawal | Record withdrawal |

Student creation and enrollment are distinct operations. Admission approval must not create duplicate Person records.

### 4.3 Fees and finance

| Method | Proposed route | Purpose |
|---|---|---|
| GET/POST | /fees/fee-heads | Fee head setup |
| GET/POST | /fees/fee-structures | Fee structure management |
| POST | /fees/fee-structures/{id}/publish | Approve/publish effective fee structure |
| POST | /fees/invoices/generate | Generate invoice batch |
| GET | /fees/invoices | Search invoices |
| GET | /fees/invoices/{id} | Invoice detail |
| POST | /fees/payments | Record payment |
| POST | /fees/payments/{id}/allocations | Allocate payment |
| POST | /fees/payments/{id}/receipt | Issue receipt after policy validation |
| POST | /fees/refunds | Request refund |
| POST | /finance/journals/{id}/post | Post balanced journal |
| POST | /finance/expenses | Record expense |
| GET | /finance/ledger | Scoped ledger report |

A payment create request must include amount, currency, channel, received-at/source reference and idempotency key. The server calculates the remaining allocation balance; clients cannot submit a trusted final ledger balance.

### 4.4 Reconciliation

| Method | Proposed route | Purpose |
|---|---|---|
| POST | /reconciliation/import-batches | Upload/import statement |
| GET | /reconciliation/import-batches/{id} | Batch status and summary |
| GET | /reconciliation/bank-transactions | Search statement rows |
| POST | /reconciliation/bank-transactions/{id}/suggestions | Generate match candidates |
| POST | /reconciliation/bank-transactions/{id}/confirm-match | Confirm reviewed match |
| POST | /reconciliation/bank-transactions/{id}/reject-match | Record rejected candidate/reason |
| POST | /reconciliation/sessions/{id}/close | Close after exceptions are accounted |

Bank file parsing must be provider/template-specific and versioned. The raw source file hash, batch and source row remain traceable.

### 4.5 Attendance and offline operations

| Method | Proposed route | Purpose |
|---|---|---|
| GET/POST | /attendance/sessions | Open/list sessions |
| GET | /attendance/sessions/{id}/roster | Minimal authorized roster |
| POST | /attendance/sync-operations | Submit one or more queued operations |
| GET | /attendance/sync-operations/{id} | Read operation acknowledgement/status |
| POST | /attendance/corrections | Request correction with reason |
| POST | /devices/register | Register an authorized device |
| POST | /devices/{id}/revoke | Revoke device and invalidate future sync |

The client displays Pending Sync until the Local API acknowledges and persists the operation. Client-supplied timestamps are descriptive evidence, not authoritative server time.

### 4.6 Examinations and results

| Method | Proposed route | Purpose |
|---|---|---|
| GET/POST | /examinations/sessions | Manage exam sessions |
| GET/POST | /examinations/assessments | Manage assessments |
| POST | /examinations/marks/bulk | Bulk marks entry |
| POST | /examinations/marks/{id}/moderate | Moderate marks |
| POST | /examinations/results/{id}/approve | Approve results |
| POST | /examinations/results/{id}/publish | Publish immutable result snapshot |
| POST | /examinations/results/{id}/revisions | Create corrected revision |
| GET | /examinations/report-cards/{id} | Read authorized snapshot |

Offline marks are drafts/queued changes. Final approval/publication requires the authorized online/local server workflow and audit.

## 5. Cloud Control Plane endpoint families

See docs/37_PLATFORM_API_CATALOG.md for the expanded BCP catalogue. Core families include:

- /public/* — published CMS content, plans, leads and demo requests.
- /registration/* — registration and verification.
- /demo/* — ephemeral synthetic demo sessions.
- /portal/* — organization-scoped commercial/node metadata.
- /admin/* — privileged internal operations with MFA and separation of duties.
- /support/* — tickets, messages and access requests.
- /webhooks/* — provider callbacks with signature validation and replay prevention.
- /licenses/* — issuance/renewal/revocation services restricted to authorized platform operations.
- /sync/* — node authentication, event delivery, acknowledgement, cursors and health.

Demo identity/session must not be upgraded to customer or Super Admin identity. Demo session IDs are not secrets and cannot authorize data access by themselves.

## 6. Idempotency and concurrency

### 6.1 Idempotency key

Use Idempotency-Key for operations where client retries could duplicate effects:
- Payment receipt and payment allocation.
- Attendance operation submission.
- Bulk marks submission.
- Bank import batch and manual match confirmation.
- Refund, waiver or approval request.
- Webhook processing.
- Sync event application.
- Licence manual import/activation.
- Data exchange package import.

The server stores key, action scope, actor/node scope, request hash, status and result reference. Same key + same request returns the original result. Same key + different request returns a conflict.

### 6.2 Optimistic concurrency

Mutable records use rowVersion or ETag/If-Match. On version mismatch, return 409 VERSION_CONFLICT with a safe current-version summary; never silently overwrite.

### 6.3 Retry

Retries use bounded exponential backoff with jitter for transient errors. Do not retry permanent validation/authorization failures indefinitely. Retry must preserve the same logical operation ID.

## 7. Integration event envelope

All cross-module or cross-node events use a versioned envelope. Payload schemas belong to the owning module and are versioned independently.

~~~json
{
  "eventId": "globally-unique-event-id",
  "eventType": "Attendance.Recorded",
  "schemaVersion": 1,
  "occurredAtUtc": "2026-11-18T10:00:00Z",
  "recordedAtUtc": "2026-11-18T10:00:01Z",
  "tenantId": "tenant-id",
  "schoolId": "school-id",
  "campusId": "campus-id",
  "originNodeId": "origin-node-id",
  "aggregateType": "AttendanceRecord",
  "aggregateId": "record-id",
  "aggregateVersion": 3,
  "actorId": "actor-id",
  "correlationId": "correlation-id",
  "causationId": null,
  "payload": {}
}
~~~

The sample values are placeholders. Do not put passwords, access tokens, unnecessary student personal data or full documents in an event envelope.

### 7.1 Required event rules

- Write the business change and Outbox event in the same local database transaction.
- Event ID is unique and stable across retries.
- Inbox processing stores the received ID before applying side effects in a transaction.
- Consumers are idempotent.
- Preserve source node, event time, server receipt time and aggregate version.
- Support schema version negotiation and reject unsupported versions with a visible exception.
- Use one-writer/aggregate-ownership rules; do not apply universal last-write-wins.
- Financial events are immutable facts; never silently merge journal lines.
- Event payload must be classified and minimized for the destination trust boundary.

### 7.2 Initial event catalogue

| Event | Owner | Consumer examples |
|---|---|---|
| Student.Admitted | Students | Fees assignment request, communication, reporting |
| Student.EnrollmentChanged | Students | Timetable/attendance scope, reporting, sync |
| Fee.InvoiceIssued | Fees | Communication, reporting |
| Fee.PaymentRecorded | Fees | Finance posting request, receipt notification, reporting |
| Fee.PaymentAllocated | Fees | Finance, household ledger projection, reporting |
| Finance.JournalPosted | Finance | Reporting, approved sync |
| Reconciliation.MatchConfirmed | Reconciliation | Fees/Finance contract, audit/reporting |
| Attendance.Recorded | Attendance | Parent notification request, analytics/sync |
| Examination.ResultPublished | Examinations | Parent notification, report read model |
| Node.LicenseRenewed | Cloud Licensing | Local entitlement snapshot activation |
| Node.SyncAcknowledged | Sync Hub | Local cursor/queue state |

This catalogue is proposed. The final owner, payload and permitted consumers must be approved per event.

## 8. File upload/download contract

- Uploads use allow-listed extensions and detected content types; filename extension alone is insufficient.
- Enforce file-size, page-count/archive-expansion and rate limits.
- Malware-scan files where supported before making them available.
- Store files outside public web roots using opaque object IDs.
- Downloads require object-level authorization and short-lived access.
- Audit sensitive file reads and exports.
- Do not leak storage bucket names, internal paths or permanent public URLs.
- Retention follows the record type and approved legal/policy schedule.

## 9. Webhook contract

Every provider webhook must:
1. Verify provider signature using the configured secret/key.
2. Validate timestamp tolerance and replay window.
3. Validate provider account/environment identity.
4. Persist provider event/message ID with a unique constraint.
5. Return quickly after durable acceptance; process expensive work asynchronously.
6. Be idempotent on duplicate delivery.
7. Store minimized payload evidence according to retention policy.
8. Never mark a fee invoice settled solely because an unverified browser return URL says payment succeeded.

Payment gateway/provider selection and exact signature algorithm remain external verification tasks.

## 10. Open API decisions

- Final route and DTO naming.
- Token issuer/audience and local identity synchronization.
- Cursor format and maximum page sizes.
- Idempotency retention duration.
- API compatibility/version sunset policy.
- Cross-module synchronous interface versus event choice for Fees→Finance posting.
- Payment provider and webhook signatures.
- External bank/payment API contracts.
- File storage provider and malware scanning tool.

## 11. Contract approval gate

- [ ] OpenAPI document generated for each host.
- [ ] Each endpoint has request/response DTOs and validation rules.
- [ ] Auth audience and permission are defined for each route.
- [ ] Tenant/campus/class/resource scope is documented.
- [ ] Idempotency and concurrency behavior is defined.
- [ ] Error codes and retry guidance are stable.
- [ ] Events have versioned schemas, owners and consumers.
- [ ] Webhook signature/replay handling is specified.
- [ ] Contract tests and API security tests are approved.
