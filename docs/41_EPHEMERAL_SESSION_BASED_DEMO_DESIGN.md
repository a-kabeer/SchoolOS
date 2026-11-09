# 41 — Ephemeral Session-Based Demo Design

**Version introduced:** v1.7 · **Status:** Accepted product direction; detailed implementation/storage decisions pending · **Implementation:** Not Started

## 1. Product decision

The instant self-service demo is a **temporary, session-bound experience**, not a permanent demo account. A visitor can explore and modify synthetic sample data only while the demo session is valid. When the visitor explicitly logs out, closes all demo tabs/windows, becomes inactive past the configured limit, or reaches the absolute session lifetime, the server revokes access and requests destruction of the temporary workspace.

The user approved this session-based temporary-demo direction on 9 November 2026. This does not mean the feature has been implemented.

## 2. Required user experience

- Starting a demo creates a new temporary demo identity/session and a fresh workspace from a versioned synthetic template.
- Users can create, edit and delete sample students, attendance, fee records and other permitted sandbox data. These changes are private to that demo session.
- Refreshing the page must not lose the demo. Reconnection during a short disconnect grace period resumes the same valid session.
- Opening multiple tabs for the same browser demo must not create separate workspaces. Closing one tab must not destroy a session while another associated tab remains active.
- Logging out explicitly revokes the session immediately and initiates high-priority workspace cleanup.
- Closing the last tab/window should trigger a best-effort disconnect signal. If the browser crashes, device loses power or the signal is blocked, server-side client leases and expiry sweeps must still end the session.
- When all associated clients disappear, use a short reconnect grace period so a page refresh is not mistaken for a deliberate abandonment. If no client reconnects, terminate the session and clean the workspace.
- A session that expires or is revoked must not be recoverable from an old browser tab, copied URL or stale token.

## 3. Lifecycle and server authority

Suggested state machine:

`Provisioning → Active → DisconnectPending → Expired/Revoked → CleanupPending → Cleaning → Cleaned`

A failed cleanup moves to `CleanupFailed` and is retried with alerting. `DisconnectPending` may return to `Active` only during the configured reconnect grace period and only if the session has not been revoked, expired or exceeded its absolute lifetime.

The server is authoritative for session state and timestamps. Browser close events are only hints; they are never the sole security or cleanup mechanism. Every demo API request must validate the server-side session status, expiry, session-bound identity and allowed demo policy. Expired/revoked requests return an authentication/session-expired response and cannot read or mutate workspace data.

### Suggested initial operating values (not final approval)

| Setting | Initial proposal | Notes |
|---|---:|---|
| Browser heartbeat | 30 seconds | Sent while a demo client is active; server timestamps receipt |
| Client lease | 90 seconds | Allows several missed heartbeats/background throttling |
| Last-client reconnect grace | 60 seconds | Refresh/multi-tab handoff protection; no access after server revocation |
| Inactivity timeout | 15 minutes | Based on server-observed user activity, not heartbeat alone |
| Absolute session lifetime | 2 hours | Maximum lifetime even if user remains active |
| Cleanup initiation on explicit logout | Immediate | Revoke first; queue cleanup at high priority |
| Cleanup sweep | Every 1 minute | Finds abandoned/expired sessions and retries eligible cleanup |

These are starting values for usability, abuse and capacity testing. They are **not confirmed commercial/service commitments** and may be tuned before launch.

## 4. Close, refresh, multiple tabs and unreliable networks

- Use `pagehide` or an equivalent lifecycle signal and a same-origin `sendBeacon`/keepalive request as a best-effort client-disconnect hint. Do not rely on `beforeunload` alone.
- Each demo tab/client receives a random, non-identifying client instance key. Store only a hash or opaque server-side identifier; do not use invasive browser fingerprinting.
- The backend tracks client leases under one demo session. A close event marks that client as disconnecting; cleanup is considered only when no valid client lease remains and reconnect grace has elapsed.
- Refresh creates a replacement client lease within the grace window and continues the existing session. It must not silently provision a second workspace.
- Browser crash, force-stop, device shutdown and network interruption are handled by lease expiry and the server sweeper.
- Inactivity timeout is separate from heartbeat: an open but idle page may still heartbeat, but the session should expire after the inactivity threshold unless the user performs an allowed interaction.
- Explicit logout is not subject to reconnect grace. Revoke the session immediately, clear the demo cookie and enqueue cleanup.

## 5. Workspace isolation and storage recommendation

### Non-negotiable boundary

- Demo data must live outside production campus operational databases and must never share production credentials or privileged connection paths.
- Every read, write, export and background job must be scoped to the server-derived `DemoSessionId`.
- A demo session can never select another session's ID to gain access; IDs are identifiers, not authorization.
- Use PostgreSQL Row-Level Security (RLS) as defence in depth if session rows share tables, plus mandatory application-level authorization and isolation tests.
- The Master Demo Template is immutable/versioned. Each session starts from a clean copy/snapshot/seed; user changes never modify the master.

### Recommended low-resource starting shape

Use a **dedicated Demo Runtime database or isolated demo data store**, separate from production and from the Cloud Control Plane's customer/billing tables. Keep session workspaces logically isolated within that demo-only store using strict server-derived session scope and RLS. This avoids provisioning a new PostgreSQL database for every visitor while ensuring demo records are not mixed into production school data.

Before launch, threat-model and load-test the implementation. A separate database/schema per session or short-lived isolated runtime may be chosen for higher isolation if concurrency, customer assurance or test results justify its resource cost. Do not use a normal production application database with informal `DemoSessionId` filters and no database-enforced isolation.

### Synthetic data only

- Use generated/fabricated school, learner, guardian, staff, fee and academic records.
- Never seed from production, customer backups, customer exports or real student lists.
- Display a visible Demo watermark and warning not to enter real student or financial information.
- Disable real payment processing, invoice settlement, email/SMS/WhatsApp delivery, production licence issuance and real campus synchronization.
- Prefer disabling arbitrary file uploads in the initial demo. If a demo use case requires uploads, store them under a session-specific temporary object prefix with size/type scanning and guaranteed lifecycle cleanup.

## 6. Cleanup and data-deletion contract

The cleanup worker must be idempotent and remove all session-owned data/resources, including:

1. Demo operational rows and session-created records in the Demo Runtime store.
2. Temporary uploads, generated exports and object-storage prefixes.
3. Cache entries, search indexes, embeddings/vector entries and temporary report artifacts, if enabled.
4. Queued background jobs, scheduled tasks and pending notifications belonging to the session.
5. Temporary authentication credentials, refresh tokens, session cookies (server-side revocation), and client leases.
6. Ephemeral environment/container resources, if an isolated runtime is introduced later.

**Deletion semantics:** Access is revoked immediately; physical cleanup is asynchronous and should run promptly through a high-priority worker. Retries must continue after transient errors, with a dead-letter/alert path for persistent failures. A user must never regain access while cleanup is pending or failed.

### Backups, logs and residual copies

- Do not place ephemeral demo data in long-lived customer/production backup sets.
- Prefer a separate demo runtime store that is excluded from normal backups because demo sessions are disposable. Keep the immutable synthetic master template separately backed up/versioned.
- If infrastructure-level snapshots or backups are unavoidable, document their short retention and deletion/expiry behavior. Do not claim immediate erasure from every backup copy when immutable snapshots still retain the data.
- Application, telemetry and audit logs must not capture demo record payloads, uploaded file contents, credentials or sensitive form contents. Keep only minimal operational metadata needed to prove session creation/expiry/cleanup and diagnose failures.
- Define and test the maximum residual-data lifetime before launch. Legal/privacy review must validate the final retention notice.

## 7. Security and abuse controls

- Give demo authentication a separate audience and cookie namespace from Customer Portal and Super Admin; never share admin or customer tokens.
- Use Secure, HttpOnly, appropriately scoped SameSite cookies over HTTPS. Protect state-changing requests against CSRF and validate origin where applicable.
- Do not trust client-supplied organization, campus, session or user scope. Resolve permissions from the server-side session record.
- Apply per-IP, per-session and global concurrency/rate limits; add bot protection where justified.
- Enforce allowed demo features/actions on the backend, not only by hiding buttons in React.
- Prevent demo access to real integrations, secrets, production files, customer records and Local Nodes.
- Admin staff may inspect operational session metadata/cleanup health, but should not browse a visitor's temporary records by default.
- Record security-relevant events without storing demo business-record contents.

## 8. Suggested platform entities

Extend the preliminary demo entity catalogue with:

- `DemoSessions`: template/version, status, created/last-activity/idle-expiry/absolute-expiry timestamps, disconnect deadline, revoked/cleanup timestamps, retry count and non-sensitive cleanup outcome.
- `DemoSessionClients`: session reference, opaque client key/hash, connect/heartbeat/disconnect and lease-expiry timestamps.
- `DemoSessionResources`: session reference, resource kind/reference, cleanup state, last attempt and retry metadata.
- `DemoSessionEvents`: minimal lifecycle/security event type and timestamp; no record payloads or secrets.

Avoid storing more visitor identity data than needed. A personalized sales-demo request is a separate CRM workflow and may retain contact information according to its consent and retention policy; it must not prolong or preserve the instant demo workspace.

## 9. Suggested APIs

Use the current `/api/v1/demo` family and add/clarify the following server-authoritative operations:

- `POST /api/v1/demo/sessions` — create a temporary session and provision a fresh workspace.
- `GET /api/v1/demo/sessions/current` — return current session status and safe expiry information.
- `POST /api/v1/demo/sessions/current/clients/connect` — register/reconnect a tab/client.
- `POST /api/v1/demo/sessions/current/heartbeat` — renew the client lease and report allowed activity state.
- `POST /api/v1/demo/sessions/current/clients/disconnect` — best-effort close signal; never trust it as the sole cleanup trigger.
- `POST /api/v1/demo/sessions/current/end` — explicit logout/revocation and high-priority cleanup request.
- `POST /api/v1/demo/sessions/current/reset` — restore this session's workspace from the immutable template.

The API should derive session identity from its secure cookie/token and server-side session record. Do not authorize access from a caller-provided `{sessionId}` alone. Existing personalized demo request APIs remain separate.

## 10. Acceptance criteria and test scenarios

- Explicit logout revokes all demo access immediately; a stale tab/token cannot continue using the session.
- Closing the last tab normally triggers cleanup after the short reconnect grace period.
- Closing one of several tabs does not terminate the session while another client remains active.
- Refresh resumes the same session/workspace during the reconnect grace period and does not create duplicate workspaces.
- Browser crash, force-stop, network loss and missed close beacon are handled by server-side client lease expiry and cleanup sweep.
- Idle timeout and absolute lifetime both expire active sessions at the configured limits.
- Expired, revoked or cleanup-pending sessions cannot read or mutate data.
- Two concurrent demo sessions cannot read, update, export, reset or delete each other's records, files, cache entries or jobs.
- A reset restores the immutable template without affecting another session or the master template.
- Cleanup is idempotent, retryable and observable; a simulated cleanup failure is surfaced to operations and retried.
- Demo records do not appear in production customer/campus databases, long-lived backups, logs or analytics payloads.
- No real payments, licences, emails, SMS, WhatsApp or campus sync can be triggered from a demo.
- Automated tests cover authorization bypass, session fixation/replay, stale tokens, RLS bypass, race conditions, multi-tab handling and cleanup failures.

## 11. Decisions still requiring implementation discovery

- Benchmark shared dedicated Demo Runtime + RLS against per-session schema/database or isolated runtime; select based on threat model, hosting cost and concurrency tests.
- Finalize heartbeat, lease, reconnect grace, inactivity timeout, absolute lifetime, concurrency limits and cleanup SLA.
- Decide whether any file-upload workflow is essential for the first demo; default recommendation is disable it.
- Confirm backup/snapshot architecture can meet the promised demo-data deletion window.
- Define the minimal retention period for non-content operational events and cleanup evidence.

## 12. Delivery status

All work is **Not Started**. This document records the approved product direction and recommended design; it does not claim that session handling, workspace provisioning, cleanup workers, backups or security controls have been implemented.
