# 05 — PWA and Offline Synchronization Architecture

> **Architecture update (v1.1):** This document originally described browser/PWA local caching. The selected product now uses a **Local Data Plane server** for broad offline operation. PWA/IndexedDB remains useful for browser drafts, short-lived caches and device-side resilience, but it is not the authoritative offline backend. Approved operations should be written to the Local Node's PostgreSQL database over the school LAN, even when the internet is unavailable. Read `14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`, `15_SUBSCRIPTION_LICENSING_AND_OFFLINE_ENFORCEMENT.md` and `16_LOCAL_SECURITY_SYNC_AND_OPERATIONS.md` first.

## 1. Goal

Allow authorized staff to continue approved workflows through a school Local Node when internet/cloud connectivity is unavailable. Browser PWA caching is secondary. The Local Node stores authoritative local records and later synchronizes business events to the Cloud Sync Hub. Offline capability remains defined per operation; not every feature is offline-capable.

## 2. Recommended client technology

- React PWA
- Web App Manifest
- Service Worker for application shell/assets and selected resources
- IndexedDB for local records and operation queue
- Dexie or equivalent IndexedDB wrapper
- Versioned local data schema and migrations
- Sync client with retry/backoff and visible status

## 3. Offline policy by workflow

| Workflow | Offline behaviour | Important limitation |
|---|---|---|
| Assigned class/student list | Cached read | Cache only assigned/minimum data |
| Student attendance | Create/update locally and queue | Server validates scope and conflicts |
| Staff attendance | Local entry and queue | Policy controls edits and duplicate punch handling |
| Marks entry | Offline draft/queue | Final approval/publication is online |
| Homework/class diary | Offline draft and queue | Attachments may remain pending |
| Timetable | Cached read | Show last-updated timestamp |
| Lesson plan | Cached read/draft | Sync on reconnect |
| Cash fee collection | Provisional local receipt and queue | Server confirmation after sync; device-level controls required |
| Parent fee statement | Limited cached read | Show stale timestamp and prevent unsafe payment claims |
| Notices | Cached read | Show last-updated time |
| Bank verification | Online only | No offline bank confirmation |
| Refund/waiver approval | Online only | Authorized server action |
| Bank reconciliation | Online only | Requires authoritative server data |
| Journal posting | Server-authoritative | No client-side final posting |
| AI operations | Usually online | Queue request only if safe; do not promise offline model execution |

## 4. Sync operation envelope

Illustrative payload (not a final API contract):

```json
{
  "operationId": "client-generated-unique-id",
  "deviceId": "registered-device-id",
  "entityType": "AttendanceRecord",
  "entityId": "record-id",
  "operationType": "Create",
  "localSequence": 102,
  "baseVersion": 4,
  "occurredAt": "2026-11-09T08:30:00Z",
  "payload": {}
}
```

The API must derive tenant, user and permitted campus/class scope from authenticated server-side context. Client-supplied tenant or campus IDs are data to validate, not authorization evidence.

## 5. Sync lifecycle

Suggested statuses:

- Local Draft
- Pending Sync
- Uploading
- Synced
- Conflict
- Rejected / Needs Review
- Retry Scheduled

A successful client upload request is not the same as a server-confirmed business operation. The client changes to Synced only after a server acknowledgement.

## 6. Required sync protections

- Unique operation ID per logical action.
- Server-side idempotency store/constraint.
- Registered device and user authorization.
- Tenant/campus/class scope validation.
- Entity version/base-version checks.
- Duplicate event detection.
- Retry with bounded exponential backoff and jitter.
- Acknowledgement and per-operation result.
- Failed-operation review queue.
- Local schema version and migration handling.
- Device revocation and local cache cleanup.
- Clock skew tolerance; client timestamps are not trusted as authoritative server time.
- Audit source, device and sync metadata.

## 7. Conflict policy

Do not apply universal last-write-wins.

Examples:
- Attendance: use policy-defined conflict detection and teacher/admin review where competing edits occur.
- Marks: protect approved/locked results; route conflicts to examination controller.
- Cash receipt: unique local transaction/receipt identifier; server checks duplicate, validity and authorized cashier/device.
- Student profile: use field-level concurrency or a review workflow for conflicting edits.
- Financial approvals: online-only server-authoritative operation.

## 8. Browser limitations to account for

- Browser background sync support and execution timing vary across platforms.
- A PWA cannot guarantee a background upload while closed on every browser/device.
- The app must synchronize on launch, foreground, reconnection and explicit user action.
- Large file uploads may need to wait for connectivity.
- Browser storage can be evicted; the application must monitor quota and protect pending data with clear warnings/export/recovery mechanisms where feasible.
- Offline local storage is not automatically a secure vault. Minimize cached data and apply device/session controls.

## 9. Offline financial receipts

A cash collection made offline may create a provisional receipt with a unique device-generated identifier. The receipt must clearly show Pending Sync / Provisional status. Server acceptance, duplicate checks and ledger posting happen on synchronization. The school must define how a cashier handles a local receipt that the server rejects or flags for review.

Bank-transfer evidence entered offline must never be labelled bank-confirmed without authoritative verification.

## 10. Offline acceptance tests

- Disconnect network during attendance entry.
- Force-close/reopen browser and PWA.
- Reconnect after several hours/days.
- Retry the same operation multiple times.
- Submit two conflicting attendance edits.
- Expire authentication while offline.
- Revoke device while it has cached data.
- Exhaust/approach local storage quota.
- Change local schema version with pending operations.
- Upload offline cash receipt twice.
- Confirm no cross-tenant operation is accepted.
- Confirm every accepted operation is acknowledged or remains visibly in an exception state.


## 11. Local-server mode

For the selected Hybrid model, local users connect to a React application hosted by the Local Node. The Local Node's ASP.NET Core Modular Monolith and PostgreSQL remain available on the private LAN while the internet is down. Local authentication and local authorization must also work during this period.

If a school chooses one shared group node, all branches must retain a private network route to it. If a campus must work while disconnected from head office, it needs its own Local Edge Node. See document 14.

## 12. Local license boundary

Local writes are allowed only while the node has a valid signed license lease or is within the approved grace window. The Local Node cannot be remotely revoked while completely offline. After the signed lease/grace expiry, it enters restricted mode. See document 15.

## 13. AI outage behaviour

Cloud-hosted AI providers are unavailable during an internet outage. Core school operations continue; AI requests may be queued. Local model execution is a separate optional package and must be benchmarked before being advertised as offline-capable.
