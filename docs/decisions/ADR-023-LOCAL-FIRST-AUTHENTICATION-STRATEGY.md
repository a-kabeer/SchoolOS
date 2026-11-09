# ADR-023 — Local-First Identity and Authentication Strategy

- **Status:** Accepted (Planning & Foundation Baseline)
- **Date:** 2026-11-09
- **Owner:** Software Architecture Team
- **Reviewers:** Technical Lead, Security Engineer

## Context

SchoolOS operates under a hybrid architecture where each school campus may run a Local Data Plane node capable of full autonomous operation during prolonged internet outages (`docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`). Teachers, administrative staff, and local operators log in to the Local Node over the school LAN.

Simultaneously, SchoolOS Cloud Control Plane serves customer registration, billing, and global administration. The identity and authentication system must satisfy both environments without introducing fragile cloud dependencies for local school logins.

This ADR evaluates identity frameworks and defines the provider-independent authentication architecture for Phase 1.

## Decision Drivers

1. **Local-First Autonomy:** Local Data Plane nodes must authenticate school staff (teachers, accountants, principals) entirely offline when the WAN is down.
2. **Standardized Token / Claims Model:** In both local and cloud environments, authorization must resolve tenant and campus scope from validated, server-side `ClaimsPrincipal` objects.
3. **Decoupled TenantContext:** The application security primitives (`ITenantContext`) must be completely independent of the concrete identity provider or token issuer.
4. **Maintenance & Complexity Budget:** A small team cannot maintain a custom cryptography/password hashing library or operate complex distributed OIDC identity servers (e.g. Duende, Keycloak) locally at each campus.
5. **No Development Bypasses:** Testing and development must use genuine claims validation rather than insecure mock authorization or header spoofing.

## Options Considered

1. **Cloud-Centralized Identity Provider Only (Auth0 / Entra ID / Cloud Keycloak):**
   - *Pros:* Fully managed, out-of-the-box SSO, MFA, social logins.
   - *Cons:* Violates the core offline requirement. If internet connectivity fails at a campus, staff cannot log in to take attendance, record fee collections, or conduct exams. Unacceptable for Pakistani school operations.
2. **Local OpenID Connect Server (Duende IdentityServer or OpenIddict on each node):**
   - *Pros:* Standard OAuth2/OIDC token flows locally.
   - *Cons:* Significant operational complexity, token refresh management across local browser sessions, database schema bloat for client/scope tables, and licensing overhead (for Duende).
3. **ASP.NET Core Identity with Local JWT / Cookie Issuance (Selected):**
   - *Pros:* Standard Microsoft security framework; robust password hashing (PBKDF2 with HMAC-SHA512), lockout protection, two-factor support, and claims generation built into the framework. Fully operational locally on PostgreSQL.
   - *Cons:* Default cookie schemes require careful configuration when consumed by SPAs/PWAs; requires explicit token issuance handlers for mobile/API clients.

## Decision

We adopt a **Two-Tier Identity Architecture with Provider-Independent Security Abstractions**:

1. **Provider-Independent Application Security (`ITenantContext`):**
   - Application controllers, services, and EF Core interceptors depend solely on `ITenantContext` and `ClaimsPrincipal`.
   - Security context never reads from user tables directly; claims (`tenant_id`, `sub`, `campus_id`, `role`, `scope`) are validated from the ambient cryptographic ticket.
2. **Local Data Plane Identity:**
   - Powered by **ASP.NET Core Identity** mapped to the local PostgreSQL database (`identity` schema).
   - Local nodes issue signed JWT tokens (or secure SameSite cookies) signed by the local node's cryptographic keypair.
   - Operates 100% offline without contacting the Cloud Control Plane.
3. **Cloud Control Plane Identity:**
   - Evaluated separately for Customer Portal and Super Admin console (OpenID Connect / OIDC federated identity or centralized ASP.NET Core Identity).
4. **Zero Authentication Bypasses:**
   - Testing uses simulated cryptographically signed tokens or claims principals generated via test helpers, ensuring real middleware pipeline execution.

## Consequences

### Positive
- School operations remain 100% functional during internet outages.
- Cryptographic security and password hashing leverage battle-tested Microsoft security libraries.
- `ITenantContext` remains cleanly decoupled; switching or extending identity mechanisms later does not alter domain modules.

### Negative / Trade-offs
- Password resets on disconnected local nodes must follow local administrative workflows (e.g. campus admin reset) rather than cloud email delivery.
- Account synchronization between campus edge nodes and cloud requires structured outbox event replication in future phases.

## Follow-up Tasks (Phase 1 Next Slices)
- [ ] Scaffold `SchoolOS.Modules.Identity` with ASP.NET Core Identity stores mapped to `identity` schema.
- [ ] Implement JWT token issuance and cookie options for local SPA clients.
- [ ] Implement role-based and campus-scoped authorization handlers.

## References
- `docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md`
- `docs/07_SECURITY_AND_QUALITY_REQUIREMENTS.md`
- Microsoft ASP.NET Core Identity documentation
