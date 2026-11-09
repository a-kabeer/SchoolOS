# 31 — Demo Platform Specification

**Version:** v1.7 · **Status:** Session-based ephemeral demo direction accepted; implementation Not Started

## 1. Demo modes (both selected)

### A. Instant self-service demo

Visitor selects a demo template (for example: single campus, multi-campus chain, or disconnected-campus scenario), starts a temporary session and receives an isolated ephemeral workspace. The environment contains synthetic organization, student, fee, attendance, timetable and examination data. Users can explore permitted screens and sample workflows only while the session remains valid. Explicit logout revokes access immediately and initiates cleanup; closing all demo tabs/windows, inactivity or absolute lifetime also ends the session through best-effort close signaling plus server-side leases and expiry sweeps. Refresh and multiple tabs must continue the same session during a short reconnect grace period. See `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021 for the full lifecycle and deletion design.

### B. Personalized sales demo request

Visitor submits school/contact profile, estimated student/campus count, current system, deployment requirement, desired modules, preferred date/time window and contact permission. The platform creates or updates a CRM lead and a sales appointment request. A company sales user confirms the slot; the form itself must not promise availability before confirmation.

## 2. Isolation and environment strategy

Use a versioned immutable synthetic master template and a fresh temporary workspace per session. The recommended starting point is a dedicated Demo Runtime database/store, separate from production campus databases and customer/billing Control Plane data, with server-derived session scope and PostgreSQL RLS. Benchmark per-session schema/database or isolated runtime alternatives before launch. Never rely on application query filters alone. Never use production tenant databases or production credentials. A dedicated demo runtime allows disposable demo data to be excluded from long-lived production/customer backups. Full trade-offs are in `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md`.

Demo runtime must have:
- Synthetic data generator and documented dataset version.
- Read-only or safe sandbox actions; no real payment gateway, SMS, WhatsApp, email blast or licence issuance.
- Configurable session TTL, inactivity timeout, absolute lifetime, client lease/heartbeat, last-tab reconnect grace, request throttling and cleanup job. Initial timing proposals are documented in `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and are not final SLAs.
- Reset operation with audit event and deterministic baseline state. Refresh reconnects to the same session; one tab closing does not terminate a session while another associated tab remains active.
- Clear demo watermark/banner and warning not to enter real student information.
- Abuse detection, bot protection and per-IP/session quotas. Separate demo auth audience/cookie namespace from portal/admin; backend revocation is authoritative.

## 3. Demo templates

- `SingleCampus`: one local school/campus with common workflows.
- `MultiCampusCloud`: several campuses with centralized access and campus-based permissions.
- `DisconnectedCampus`: independent campus nodes with imported data snapshot freshness indicators.
- Optional later template: `HybridSync`: local nodes and simulated sync health. It must be clearly labelled simulated unless real infrastructure exists.

## 4. Entities

`DemoTemplates`, `DemoTemplateVersions`, `DemoSessions`, `DemoSessionUsers`, `DemoEnvironments`, `DemoEnvironmentResources`, `DemoResetJobs`, `DemoSessionEvents`, `DemoRequests`, `DemoRequestPreferences`, `SalesAppointments`, `DemoAccessPolicies`.

Each session records template/version, start/end/idle/absolute expiry, status, opaque session identity, connected client leases, environment/resource references, data reset version and cleanup status. Avoid collecting personal data beyond what is needed for a requested sales follow-up. Add `DemoSessionClients`, `DemoSessionResources` and minimal `DemoSessionEvents`; see document 41 for suggested fields and lifecycle states.

## 5. Routes and APIs

Routes: `/demo`, `/demo/select-environment`, `/demo/session`, `/demo/dashboard`, `/demo/modules`, `/demo/request-personalized`.

APIs:
- `GET /api/v1/demo/templates`
- `POST /api/v1/demo/sessions`
- `GET /api/v1/demo/sessions/current`
- `POST /api/v1/demo/sessions/current/reset`
- `POST /api/v1/demo/sessions/current/clients/connect`
- `POST /api/v1/demo/sessions/current/heartbeat`
- `POST /api/v1/demo/sessions/current/clients/disconnect` (best-effort close hint only)
- `POST /api/v1/demo/sessions/current/end` (explicit logout/revocation)
- `POST /api/v1/demo/requests`
- `GET /api/v1/demo/requests/{requestId}` (authorized requester/staff only)

Demo actions must verify the separate demo session credential, server-side status/expiry, template policy and rate limit. Resolve session identity from the authenticated server-side record; never authorize using a caller-supplied `{sessionId}`, organization or campus ID. Explicit logout revokes immediately; close beacons are best-effort and server-side client lease/expiry sweeps are mandatory. Cleanup covers DB rows, temporary objects/files, cache, queues, search/vector resources and ephemeral runtimes. Exclude disposable demo data from long-lived production/customer backups and do not log demo record payloads.

## 6. Acceptance tests

- A demo user cannot enumerate another session or access production API resources.
- Expired/revoked sessions are denied even when a stale browser tab remains open; explicit logout invalidates access before physical cleanup completes.
- Last-tab close, browser crash, network loss and missed close beacon are handled by server-side lease expiry and cleanup sweep.
- Page refresh and multiple tabs preserve one session/workspace within reconnect grace; closing one tab does not destroy another active tab’s session.
- Cleanup retries on injected failures and removes associated temporary files, cache, queues and runtime resources.
- Disposable demo rows are absent from production/customer databases and long-lived backup sets.
- Reset restores seeded data and does not affect another session.
- No real customer/student/guardian records appear in demo storage or logs.
- Demo cannot issue a real invoice, collect payment, send real bulk messages or create a production licence.
- Personalized request creates a CRM activity and is visible only to authorized sales users and the requester as permitted.


## Session-based demo decision update (v1.7)

The instant demo is now defined as an ephemeral session-bound workspace, not a persistent demo account. Logout immediately revokes the session and queues cleanup; closing the last tab is detected best-effort and backed by server-side client leases, idle/absolute expiry and periodic cleanup. Refresh and multi-tab support use a short reconnect grace. See `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021. All implementation remains Not Started; proposed timeout values and the final storage isolation mechanism remain subject to tests and approval.
