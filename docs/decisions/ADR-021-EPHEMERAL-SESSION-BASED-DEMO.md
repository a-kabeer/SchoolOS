# ADR-021 — Ephemeral Session-Based Demo

- **Date:** 9 November 2026
- **Status:** Accepted product direction; implementation design details pending
- **Related:** `docs/31_DEMO_PLATFORM_SPECIFICATION.md`, `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md`, BCP-4

## Context

SchoolOS needs an instant self-service demo. A permanent demo account or shared mutable demo environment can accumulate data, allow visitors to affect one another and require ongoing cleanup. The desired experience is temporary: the demo should exist only while a visitor is actively using it, and its temporary data should be removed after logout, page abandonment, inactivity or maximum lifetime.

Browser shutdown is not a reliable signal. A tab can be refreshed, multiple tabs can be open, a browser can crash, and a device can lose power. Therefore a client-side unload callback cannot be the only cleanup mechanism.

## Decision

1. Instant demo is a **session-bound ephemeral workspace**, not a permanent demo account.
2. Each session receives a fresh workspace based only on an immutable/versioned synthetic dataset.
3. Demo workspace data is isolated from production campus databases and customer/commercial Control Plane data.
4. Demo authentication has its own audience/cookie namespace and server-side session record.
5. Explicit logout revokes access immediately and starts high-priority cleanup.
6. Last-tab/browser close is detected best-effort; server-side client leases, inactivity expiry, absolute lifetime and a periodic cleanup sweeper provide reliable fallback.
7. Refresh and multi-tab use are supported through client leases and a short reconnect grace period.
8. Cleanup covers database records, temporary files/objects, cache, queues, search/vector artifacts and temporary runtime resources. Cleanup is idempotent and retryable.
9. Ephemeral demo data is excluded from long-lived production/customer backup sets. The synthetic master template is maintained separately.
10. No real school/student data or production integrations are available inside the demo.

## Recommended implementation direction

Start with a dedicated Demo Runtime database/store separate from production and the customer/billing Control Plane. Use server-derived `DemoSessionId` scope and database-enforced RLS for session isolation. Benchmark against per-session schema/database or ephemeral runtime isolation before launch. Do not rely on application query filters alone and do not put disposable records into production operational databases.

## Consequences

### Benefits
- Visitors receive a clean and private sample environment.
- Session data does not accumulate as a permanent customer-like account.
- Explicit logout and server-side expiry make access revocation enforceable.
- The master synthetic template remains intact.
- Production school data and integrations remain outside demo trust boundaries.

### Costs and trade-offs
- Workspace provisioning and cleanup require background processing, monitoring and retry logic.
- Browser close cannot be detected with absolute immediacy; a short grace period and server lease expiry are required.
- Multi-tab and refresh semantics need careful lifecycle tests.
- Backup/snapshot design must be considered to avoid overstating deletion guarantees.
- Per-session databases or containers may cost more resources; the first implementation must benchmark isolation and cost.

## Proposed values, not final commitments

Initial test values: heartbeat 30 seconds, client lease 90 seconds, last-client reconnect grace 60 seconds, inactivity timeout 15 minutes, absolute lifetime 2 hours, cleanup sweep every 1 minute. Tune these after usability, security and load tests. They are not approved customer-facing SLAs.

## Open implementation questions

- Final storage isolation mechanism and capacity limits.
- Exact timeouts, concurrency and cleanup completion target.
- Upload support and ephemeral object-storage policy.
- Backup/snapshot exclusions and maximum residual-data lifetime.
- Minimal operational event retention and legal/privacy wording.

## Verification required

Automated tests must cover logout revocation, close/crash fallback, refresh, multi-tab, inactivity and absolute expiry, cross-session isolation, RLS, cleanup retries, no production-data access and no real integration side effects.
