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

### 2. Password Hashing Algorithm Evaluation & Selection
- **Evaluation:**
  - *Argon2id:* Memory-hard and state-of-the-art against ASIC/GPU cracking. However, it requires native C libraries / FFI wrappers (`libsodium` or `Konscious.Security.Cryptography`), adding unmanaged binary dependencies and compilation overhead across heterogeneous platforms (Windows x64 desktop, Linux Alpine containers, ARM64 local edge gateways).
  - *ASP.NET Core Identity PasswordHasher (PBKDF2-HMAC-SHA512):* Built into the standard .NET 10 runtime (`Microsoft.AspNetCore.Identity`), requires zero external packages, complies fully with NIST SP 800-63B guidelines, and provides native, battle-tested constant-time verification with 100% cross-platform deterministic portability.
- **Decision:** Adopt **ASP.NET Core Identity `PasswordHasher<User>` (PBKDF2-HMAC-SHA512)**.
- **Cryptographic Parameters:**
  - Algorithm: PBKDF2 with HMAC-SHA512 (Identity Version 3 format).
  - Salt Size: 128 bits (16 bytes) generated via `RandomNumberGenerator`.
  - Iteration Count: 100,000 iterations (balanced for 15-30ms verification time on edge hardware).
  - Subkey Size: 256 bits (32 bytes).
  - Verification: Constant-time bitwise comparison to prevent timing side-channel attacks.
  - Custom Cryptography Ban: No custom hashing, salting, or comparison code is permitted.

### 3. Password Change, Reset & Offline Voucher Workflow
- **Connected Mode:** Password updates are verified against the existing password, hashed via `PasswordHasher<User>`, `CredentialVersion` is incremented, and updates are queued in the transactional Outbox for encrypted replication.
- **Disconnected Local Mode:** During WAN outages, cloud-based email/SMS reset workflows are unavailable. A designated local administrator (`CampusAdmin` or `Principal`) can initiate a supervised offline credential reset. The local node sets `MustChangePasswordOnNextLogin = true`, generates a temporary password, increments `CredentialVersion`, and records a high-severity `EmergencyCredentialResetEvent` in the transactional Outbox.
- **Conflict Resolution on Reconnect:** When the local node reconnects, outbox synchronization resolves password updates using monotonic credential version numbers. The higher version wins. If equal version concurrent changes occur, cloud administrative policy takes precedence.

### 4. Separate Cloud and Local Authentication Control Planes
- **Cloud Control Plane (`SchoolOS.Cloud.Api`):**
  - Issuer: `https://cloud.schoolos.internal`
  - Audience: `schoolos-cloud-control-plane`
  - Signing Keys: RSA 4096-bit or ECDSA P-256 asymmetric keypairs managed via cloud key vault. Key identifier header (`kid: cloud-2026-v1`) embedded in JWT headers.
  - Scope: Platform administrators, customer onboarding, tenant subscription management.
- **Local Data Plane (`SchoolOS.Local.Api`):**
  - Issuer: `https://campus-{campusId}.schoolos.local`
  - Audience: `schoolos-local-data-plane`
  - Signing Keys: Campus node RSA 2048-bit or ECDSA P-256 asymmetric keypairs generated at node provisioning. Private key secured via platform DPAPI / Linux Keyring. Key identifier header (`kid: campus-{campusId}-v1`).
  - Validation: 100% autonomous local validation using the node's local public key. Local school login does not ping or require cloud infrastructure.

### 5. Refresh Token Rotation, Storage & Reuse Detection
- **Storage:** Refresh tokens are never stored in plaintext. The database persists only `SHA-256(raw_refresh_token)` in `identity.user_sessions`.
- **Rotation:** Every refresh token exchange issues a new refresh token and invalidates the previous token.
- **Reuse / Breach Detection:** If a previously consumed or revoked refresh token is presented:
  1. The server flags a critical security breach (`TokenReuseDetected`).
  2. The parent `UserSession` is immediately revoked across all sibling tokens (`IsRevoked = true, RevokedReason = 'TokenReuseDetected'`).
  3. A high-priority security audit event is emitted.
  4. The request is rejected with HTTP 401 Unauthorized, forcing user re-authentication.

### 6. Credential Versioning & Stale Token Invalidation
- Every user entity maintains an integer `CredentialVersion` and GUID `SecurityStamp`.
- Every password change, administrative lockout, or session revocation increments `CredentialVersion`.
- Every issued access token carries the claim `cv: <int>`.
- The authentication middleware verifies `token.cv == user.CredentialVersion`. If `token.cv < user.CredentialVersion`, the token is rejected immediately, providing sub-millisecond global token revocation across all devices without requiring distributed blacklist caches.

### 7. Tenant and Campus Context Switching
- Client requests a context switch via `POST /api/v1/auth/switch-context` with `targetCampusId`.
- The server validates that `UserId` possesses an active, unrevoked assignment in `identity.user_campus_assignments` for that specific `(tenant_id, target_campus_id)`.
- If verified, the server updates `UserSession.ActiveCampusId` and issues a newly scoped JWT access token.
- **Zero Client-Trust:** Server authorization policies NEVER trust client-supplied scope headers (`X-Campus-Id` or `X-Tenant-Id`) without validating the cryptographically signed JWT claims or executing database membership checks.

### 8. Frontend Token Security & Session UX
- **Access Token:** Kept strictly in React application memory / closure state. Never persisted in `localStorage` or `sessionStorage` where malicious third-party scripts or XSS could extract it.
- **Refresh Token:** Stored in an `HttpOnly`, `Secure`, `SameSite=Strict` cookie path-restricted to `/api/v1/auth/refresh`.
- **Silent Renewal:** API client automatically dispatches a silent refresh request 60 seconds prior to token expiration.
- **Session Expiration UX:** Unobtrusive modal alert when session is about to expire; graceful redirect to `/auth/login?returnUrl=...` on termination.

### 9. Revocation and Disabled-User Propagation
- When a user is terminated, suspended, or locked out in the Cloud Management Console:
  1. The Cloud node immediately revokes active cloud sessions and updates `IsActive = false`.
  2. A high-priority `UserSuspendedEvent` is dispatched via the sync pipeline.
  3. Upon receipt by the local node, the user record is updated to `IsActive = false`, and the `SecurityStamp` is regenerated.
  4. Active local JWT access tokens (short-lived: 15 minutes) expire, and subsequent token refresh or login attempts fail closed.

### 10. Local Offline MFA (RFC 6238 TOTP)
- Where multi-factor authentication is required for financial or exam operations, local nodes utilize **RFC 6238 TOTP** (e.g. Google Authenticator / Microsoft Authenticator). TOTP operates 100% offline because it relies on standard Unix epoch time calculations and pre-shared HMAC keys, requiring only synchronized system clocks.

### 11. Threat Modeling: Consequences of a Compromised Local Node
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
