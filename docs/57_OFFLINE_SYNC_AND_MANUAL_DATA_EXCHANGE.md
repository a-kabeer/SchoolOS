# Offline Synchronization and Manual Data Exchange

- **Document ID:** ARC-SYNC-001
- **Status:** Detailed companion specification; review required
- **Decision maturity:** Local Node authority and operation-specific offline policy are accepted direction; workflow-specific approval/settlement semantics listed below remain open
- **Owner:** Technical lead (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `1f84bb022816a91d985c7c0baae77b3a5ca47121`; reconciled from the uploaded v2.1 remediation draft
- **Related ADRs:** ADR-013, ADR-014

## 1. Local-first operational rule

The Local Data Plane PostgreSQL database is authoritative for approved local school operations. A browser PWA may cache application assets, hold uncommitted drafts or provide device-side resilience, but it is not the authoritative offline backend. A LAN-connected browser continues to use its Local Node when the public internet/WAN is unavailable.

Offline eligibility is operation-specific. `Local Node available over LAN` and `public internet/cloud available` are separate connectivity conditions. A successful Local Node write is not the same as cloud synchronization acknowledgement.

## 2. Workflow policy and authority

| Workflow | Local availability during WAN outage | Required user-visible state / rule | Authority and sync |
|---|---|---|---|
| Assigned class/student lists, timetable, notices, parent statement | Limited cached read | Show last-updated/as-of time; restrict to assigned/verified scope; do not present stale balances or records as live | Local Node owns current school record; browser stores a limited projection |
| Student/staff attendance | Write through reachable Local Node over LAN | Device-only work remains `Local Draft` / `Pending Local Submit`; never label it `Synced` before receiver acknowledgement | Local Node commits record and Outbox event; conflicts/duplicate punches enter review |
| Marks entry | Drafting through reachable Local Node may continue; device-only draft remains unsubmitted | Draft is not an official mark; final approval/publication must not be inferred from a queued write | Examination service/authorized reviewer owns approval; whether Local Node can satisfy the source's “online final approval” during WAN outage remains open |
| Homework, lesson plans and attachments | Local Node draft/write where available; device draft may be retained | Show pending submission and attachment transfer state; no claim that unsent attachments are stored centrally | Academics module on Local Node after acceptance; retry uploads safely |
| Cash fee collection | Provisional collection only under approved cashier/device controls | Unique local receipt ID; show `Provisional` / `Pending Sync`; never imply bank confirmation or cloud acknowledgement | Fees/Finance command validates, deduplicates and reconciles; exact Local Node-final versus cloud-acknowledged boundary remains open |
| Bank verification, refund/waiver approval, bank reconciliation | Online/authoritative-source path only | Keep `Unverified`, `Pending Approval` or review state when unavailable; never infer success from a slip, screenshot or queued request | Authorized server-side workflow and bank/provider evidence |
| Journal posting | Server-authoritative through Local Node, not browser queue | Browser queue cannot itself post a journal; if Local Node is unavailable, no final posting | Finance service; balanced immutable posting, controlled reversal/adjustment, transactional Outbox |
| AI operations | Cloud AI unavailable during WAN outage; optional local inference only if separately installed and approved | Do not imply that cloud inference ran offline; queue only safe, privacy-approved, expiring jobs | Provider Gateway or approved campus-local runtime; core school operations remain independent of AI |

This table is a policy guide, not an authorization to make new workflows offline-capable. Where a source row leaves a rule ambiguous, this document preserves it as an open decision.

## 3. Two-stage submission and synchronization lifecycle

The product has two distinct delivery legs: device/PWA to Local Node, then Local Node to Cloud Sync Hub or an approved receiver.

| State | Meaning | Authority |
|---|---|---|
| `Local Draft` | Uncommitted user work held in browser/PWA | Not accepted |
| `Pending Local Submit` | Device request waiting to reach Local Node | Not accepted |
| `Uploading` | One identified delivery leg is in transit | No new acknowledgement |
| `Local Node Accepted` | Local Node committed the business command | Authoritative for its configured campus scope, subject to workflow policy |
| `Pending Sync` | Local Node Outbox event is durable and waiting for receiver acknowledgement | Accepted locally; not cloud-synchronized |
| `Retry Scheduled` | Transient failure has a bounded retry/backoff schedule | Depends on prior accepted state |
| `Synced` | Cloud Sync Hub/designated receiver durably acknowledged the event | Acknowledged at receiving scope |
| `Conflict` | Version, ownership or semantic conflict requires review | No resolution implied |
| `Rejected / Needs Review` | Server refused/quarantined operation | Not accepted; reason and recovery path retained |

`Local Node Accepted` and `Pending Local Submit` make the two delivery legs explicit. Existing states `Local Draft`, `Pending Sync`, `Uploading`, `Synced`, `Conflict`, `Rejected / Needs Review` and `Retry Scheduled` remain represented. An HTTP request being sent is never sufficient to mark a business operation `Synced`.

## 4. Sync envelope and protections

An illustrative operation envelope may contain `operationId`, `deviceId`, `entityType`, `entityId`, `operationType`, `localSequence`, `baseVersion`, `occurredAt` and `payload`. It is not a finalized API contract. Tenant, user and permitted campus/class scope must be derived from authenticated server-side context; client-supplied scope is validated data, not authorization evidence.

Required protections:

- Unique operation ID and server-side idempotency constraint for each logical action.
- Registered device/user authentication and tenant/campus/class scope validation.
- Entity version/base-version checks, duplicate detection and explicit per-operation acknowledgement.
- Bounded exponential retry/backoff with jitter; failed-operation review queue.
- Local schema version/migration handling and safe treatment of pending operations.
- Device revocation, cache cleanup and clock-skew tolerance; client timestamps are not authoritative server time.
- Audit provenance for actor, device, source node, event and sync result.
- No universal last-write-wins: attendance/marks conflicts require policy review; financial approvals are server-authoritative; posted journals are immutable.

## 5. Manual disconnected-campus exchange

A campus export package must be:

- Signed and encrypted for the intended recipient.
- Versioned with a manifest, stable IDs and schema-compatibility metadata.
- Incremental/watermarked where supported.
- Bound to an authenticated source node/campus and intended destination.
- Dry-runnable before import, duplicate-safe/idempotent and provenance/audit-preserving.
- Recoverable if import fails part-way, with a recorded result and safe retry path.

Imported branch data is a read-only reporting replica by default. Branch-owned corrections require an explicit correction-command and round-trip workflow. Head-office reports must display last-imported timestamp and source freshness.

## 6. Security, operations and browser limits

- Local Node initiates outbound sync; no default inbound public management port is opened.
- Authenticate nodes with managed certificates/identities and rotate/revoke credentials.
- Apply rate/size limits, schema allow-lists and payload validation; protect against tampering, replay and cross-tenant/campus routing.
- Encrypt data in transit and protect locally persisted queues.
- Monitor queue age, retries, dead letters, cursor lag, quarantines and last successful sync.
- Keep customer data out of logs; retain sufficient metadata for diagnosis/audit.
- Browser background sync and timing vary. A PWA cannot guarantee upload while closed; retry on launch, foreground, reconnection and explicit user action.
- Browser storage may be evicted and is not automatically a secure vault. Minimize cached data and show recovery warnings where feasible.
- WAN loss does not mean Local Node loss. A campus without a LAN route to its shared node cannot perform those server-side workflows unless it has its own Local Edge Node.

## 7. Open policy clarifications

1. Does “online final approval” for marks mean a reachable Local Node API or a connected Cloud Control Plane?
2. Is a Local Node-accepted cash receipt campus-final during WAN loss, or does it remain provisional pending cloud acknowledgement?
3. What age/size limits apply to parent fee-statement and timetable caches?
4. What correction window and role rules govern attendance/staff-attendance conflict review?
5. Which AI jobs may be queued, what is their expiry, and which data classes may be retained locally?

Until these questions are approved, preserve the pending/provisional states and do not imply cloud confirmation.

## 8. Acceptance tests

- Disconnect WAN while Local Node remains reachable; perform an approved attendance entry and prove it commits locally.
- Disconnect Local Node/LAN; prove browser-only work remains a draft, not an accepted record.
- Force-close/reopen PWA; restore or clearly report unsent drafts.
- Reconnect after an outage and replay queued operations; prove retries produce one business effect.
- Submit conflicting attendance edits; prove review state is visible.
- Expire authentication or revoke a device while cached data exists.
- Approach/exhaust browser storage quota and change local schema while operations are pending.
- Submit the same provisional cash receipt twice; prove duplicate handling and provisional labelling.
- Attempt bank verification, refund/waiver approval and bank reconciliation while offline; prove they cannot be marked verified/approved.
- Attempt journal posting from browser queue; prove it cannot bypass Local Node Finance command.
- Verify no cross-tenant operation is accepted and every accepted operation is acknowledged or remains in an explicit exception state.

## 9. Relationship to existing specifications

This is the detailed policy companion to [05 — PWA and Offline Synchronization Architecture](05_OFFLINE_PWA_AND_SYNC.md). Documents 14, 16, 45 and 48 remain authoritative for Local Node/Cloud Sync Hub architecture, event contracts and operations. Document 15 and ADR-012 remain authoritative for licence enforcement. This document does not silently resolve any open policy above.
