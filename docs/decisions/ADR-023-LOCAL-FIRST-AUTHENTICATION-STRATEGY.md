# ADR-023 — Local-First Identity and Authentication Strategy

- **Status:** Accepted (Planning & Foundation Baseline — Updated for Phase 1A.1)
- **Date:** 2026-11-09 (Updated: 2026-11-10)
- **Owner:** Software Architecture Team
- **Reviewers:** Technical Lead, Security Engineer

## Context

SchoolOS operates under a hybrid architecture where each school campus runs a Local Data Plane node capable of autonomous operation during prolonged internet outages (`docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`). Teachers, administrative staff, and local operators log in to the Local Node over the campus local area network (LAN).

Simultaneously, SchoolOS Cloud Control Plane serves customer registration, billing, global analytics, and multi-tenant management. The identity and authentication architecture must satisfy both environments without introducing fragile cloud dependencies for local school logins.

This ADR defines the identity architecture, threat boundaries, and credential lifecycle rules, with specific evaluation of offline authentication and password-hash replication risks.

## Decision Drivers

1. **Local-First Autonomy:** Local Data Plane nodes must authenticate school staff (teachers, accountants, principals) entirely offline when the WAN is disconnected.
2. **Strict Credential Containment:** A compromised campus node must never expose credentials or grant access to other school campuses or the Cloud Control Plane.
3. **No Naive Password-Hash Synchronization:** Raw password hashes (e.g. bcrypt/Argon2/PBKDF2 hashes) must not be broadcast or mirrored across nodes indiscriminately.
4. **Standardized Token / Claims Model:** In both local and cloud environments, authorization must resolve tenant and campus scope strictly from validated, server-side `ClaimsPrincipal` objects.
5. **Decoupled Application Security (`ITenantContext`):** Application controllers, domain services, and EF Core interceptors must depend solely on provider-independent abstractions (`ITenantContext`).
6. **Robust Revocation Propagation:** Terminated or suspended staff must have access revoked rapidly across all active sessions upon reconnection.

## Options Considered

1. **Cloud-Centralized Identity Provider Only (Auth0 / Entra ID / Cloud Keycloak):**
   - *Pros:* Fully managed, out-of-the-box SSO, MFA, social logins.
   - *Cons:* Violates the core offline requirement. During internet outages, local staff cannot log in to take attendance, record fee collections, or conduct exams. Unacceptable for school operational continuity.
2. **Distributed Password-Hash Mirroring (Cloud to All Local Nodes):**
   - *Pros:* Appears superficially simple: replicate the cloud user table to every local node.
   - *Cons:* **High Security Risk.** If an adversary obtains physical access to a single campus mini-PC or extracts its disk, they acquire the password hashes of all users in the system (or group). Offline dictionary and hashcat attacks could compromise administrative credentials globally. **Rejected.**
3. **Scoped Two-Tier Identity Architecture with Monotonic Credential Versioning (Selected):**
   - *Pros:* Each local node holds credentials *only* for staff assigned to that specific campus. No global hashes are stored locally. Credentials are versioned via `SecurityStamp` / `CredentialVersion`. Authentication is 100% offline-capable via local ASP.NET Core Identity.
   - *Cons:* Cross-campus roaming staff require explicit multi-campus credential assignment.

## Decision

We adopt a **Scoped Two-Tier Identity Architecture with Strict Credential Isolation and Versioning**:

### 1. Credential Ownership & Storage Boundaries
- **Cloud Control Plane (`platform` / `identity` schema):** Authoritative source of truth for global tenant subscriptions, platform super-administrators, and cloud portal logins.
- **Local Data Plane (`organization` / `identity` schema):** Authoritative local verification authority for campus staff. Stores credentials *only* for users explicitly assigned to that campus.
- **No Global Credential Synchronization:** The Cloud Control Plane never pushes credentials of Tenant A to Tenant B, nor does it push credentials of Campus 1 staff to Campus 2 nodes unless a user holds an explicit cross-campus assignment.

### 2. Password Change and Reset Handling
- **Connected Mode:** Password updates are initiated on either plane, verified against current password, re-hashed using ASP.NET Core Identity password hasher (PBKDF2 with HMAC-SHA512 / Argon2id), and synchronized to the user's assigned node via encrypted outbox replication.
- **Disconnected Local Mode:** During an internet outage, a local campus administrator (Principal / IT In-charge) can initiate an emergency temporary password reset for local staff. The node generates a new salt/hash, increments the local `CredentialVersion`, and records an audit event in the transactional Outbox.
- **Conflict Resolution on Reconnect:** When the local node reconnects, outbox synchronization resolves password updates using monotonic credential version numbers. The higher version wins. If equal version concurrent changes occur, cloud administrative policy takes precedence.

### 3. Revocation and Disabled-User Propagation
- When a user is terminated, suspended, or locked out in the Cloud Management Console:
  1. The Cloud node immediately revokes active cloud sessions and updates `IsActive = false`.
  2. A high-priority `UserSuspendedEvent` is dispatched via the sync pipeline.
  3. Upon receipt by the local node, the user record is updated to `IsActive = false`, and the `SecurityStamp` is regenerated.
  4. Active local JWT access tokens (short-lived: 15–30 minutes) expire, and subsequent token refresh or login attempts fail closed.

### 4. Offline Authentication Behavior
- Local nodes execute authentication 100% locally against PostgreSQL `identity.users`.
- Verification requires zero WAN connectivity, zero DNS resolution, and zero external cloud pings.
- The local node issues signed JWT access tokens or HTTP-only SameSite cookies signed by the node's local asymmetric private key (persisted in DPAPI/secure local key store).

### 5. Credential Versioning & Session Invalidation
- Every user entity maintains an integer `CredentialVersion` and GUID `SecurityStamp`.
- Every password change, lockout, or permission change increments `CredentialVersion`.
- Every issued token carries the `sec_ver` claim matching the active `CredentialVersion`.
- The `TenantContext` validation pipeline verifies that the incoming token's `sec_ver` matches the active user security stamp, immediately invalidating stale tokens across all devices.

### 6. MFA and Session Boundaries
- **Local Offline MFA:** Where multi-factor authentication is required for financial or exam operations, local nodes utilize **RFC 6238 TOTP** (e.g. Google Authenticator / Microsoft Authenticator). TOTP operates 100% offline because it relies on standard Unix epoch time calculations and pre-shared HMAC keys, requiring only synchronized system clocks.
- **Session Duration:**
  - Local Access Tokens: 30 minutes lifetime.
  - Refresh Tokens: 12 hours lifetime (sliding window, bound to device fingerprint).

### 7. Threat Modeling: Consequences of a Compromised Local Node
- If a campus server is physically stolen or root-compromised:
  1. **Blast Radius Confined to Single Campus:** The adversary obtains access only to that specific campus's local data and the credentials of local staff assigned to that campus.
  2. **No Pivot to Cloud or Other Campuses:** The local node's mTLS certificate and API token grant access only to its own tenant/campus endpoints. The node has zero credentials or network routing for any other school.
  3. **Instant Cloud Revocation:** Cloud administrators can immediately revoke the node's mTLS certificate and suspend its software license lease in the `PlatformDbContext` tenant registry. Any sync attempts from the stolen node are permanently rejected with HTTP 403 Forbidden.

## Consequences

### Positive
- 100% offline operational continuity for all school teaching and administrative functions.
- Prevents catastrophic credential leaks: compromising one campus does not compromise the school group or cloud platform.
- Defense-in-depth: combines cryptographic token validation, monotonic version revocation, and localized data boundaries.

### Negative / Trade-offs
- Local emergency password resets require administrative audit and reconciliation upon WAN reconnection.
- Multi-campus floating teachers require synchronization to each campus node they teach at.

## References
- `docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`
- `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md`
- `docs/decisions/ADR-022-IDENTIFIER-STRATEGY.md`
