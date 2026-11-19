# 62 — SaaS Tenant Architecture and Implementation Blueprint

- **Document ID:** DOC-62
- **Status:** Draft — implementation blueprint
- **Decision maturity:** Concrete v1 implementation proposal; formal owner/security approval remains required
- **Owner:** Product owner and technical lead (named assignees TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** a-kabeer/SchoolOS main @ 898812f89e0ec7f03aff6c3b1408e46782814dfd; adoption references pinned in DOC-61
- **Companions:** DOC-03, DOC-07, DOC-14, DOC-36, DOC-38, DOC-43, DOC-44, DOC-45, DOC-46, DOC-47, DOC-48, DOC-51 and DOC-60

> **Scope and truthfulness:** This document specifies how to implement the SaaS tenant foundation in SchoolOS. It is not evidence that tenant provisioning, row-level security, authentication, licensing or production deployment code already exists. Implementation status must be established by code, migrations, automated tests and operational evidence.

## 1. Purpose and target outcome

SchoolOS is a **hybrid, multi-tenant SaaS product**. SchoolOS operates a shared Cloud Control Plane for commercial/platform services. Each customer tenant operates one or more Local Data Plane nodes for authoritative school operations. A customer must be able to register, be provisioned, invite users, create its school/campus hierarchy, subscribe to capabilities, enrol Local Nodes, and continue approved operations when disconnected from the cloud.

This blueprint defines the v1 implementation boundary and contracts for:

1. Tenant identity and organization hierarchy.
2. Global user identity and tenant membership.
3. Tenant context resolution on every request.
4. Cloud database isolation and Local Data Plane separation.
5. Tenant provisioning and lifecycle.
6. Role, permission, campus and resource scope.
7. Subscription entitlements and signed Local Node licence claims.
8. Local Node enrollment, credentials and revocation.
9. Tenant-aware synchronization and audit.
10. Security, testing, rollout and operational acceptance.

The required result is not merely a tenant selector in the UI. It is an enforceable boundary across database rows, API endpoints, files, reports, background jobs, sync messages, exports, caches, logs and support access.

## 2. V1 architecture decisions proposed by this blueprint

These are the concrete defaults recommended for the first implementation. They remain proposals until the accountable owners approve the relevant DEC items in DOC-60.

| Concern | V1 implementation baseline |
|---|---|
| Commercial tenant root | One customer organization / school group is one Tenant |
| Canonical tenant identity | The immutable UUID of organizations.Organizations; this same UUID is used as TenantId in Local Data Plane records |
| Legal hierarchy | Tenant 1-to-many Schools (legal/operating school entities); School 1-to-many Campuses |
| User identity | Globally unique user identity with explicit, separately managed membership in each tenant |
| Cloud database | Shared PostgreSQL database with explicit tenant ownership columns, composite constraints and PostgreSQL RLS defense in depth |
| Local database | Separate operational database per Local Node; one Local Node is bound to exactly one tenant |
| Node topology | A tenant may have multiple nodes; one node may serve one or several campuses belonging to the same tenant |
| Cross-tenant node sharing | Not supported in v1 |
| Authorization | Default deny; validated tenant membership + permission + school/campus/resource scope + workflow state |
| Tenant selection | Validated server-side membership and tenant route/host resolution; never trust a caller-supplied tenant header or request-body field |
| Offline identity | Local identity projection and Local Node authentication remain usable during Cloud/WAN outage |
| Commercial access | Cloud subscription state produces versioned entitlement snapshots and signed node-bound lease claims |
| Tenant data in Cloud | Commercial/platform metadata and only explicitly approved, minimum-necessary projections; no uncontrolled copy of the full student/finance database |

### 2.1 Tenant, organization and ID terminology

The product term **Tenant** and the Cloud Control Plane domain term **Organization** identify the same customer boundary in v1.

- organizations.Organizations.Id is the canonical tenant UUID.
- A Cloud row's OrganizationId foreign key and a Local Data Plane row's TenantId value refer to that exact same UUID.
- Do not create a second independent Tenants table or generate a different tenant UUID unless a later approved ADR introduces a distinct billing-account-versus-operating-organization concept.
- A tenant ID is immutable. A display name, domain, slug, plan or subscription can change without changing the tenant ID.
- Do not infer tenant identity from a school name, campus code, email domain or user-provided text.

This convention preserves the existing Cloud organization catalogue and Local Data Plane TenantId fields without creating two competing customer roots.

### 2.2 Organization hierarchy

V1 cardinality:

    Tenant / Organization
      ├── School / Legal Entity A
      │     ├── Campus A1
      │     └── Campus A2
      └── School / Legal Entity B
            └── Campus B1

    Tenant / Organization
      ├── Local Node 1 ── serves Campus A1 and Campus A2
      └── Local Node 2 ── serves Campus B1

Rules:

- Every School belongs to exactly one Tenant.
- Every Campus belongs to exactly one School and therefore exactly one Tenant.
- An organization that operates only one legal school still creates one School record; do not collapse School and Tenant IDs.
- Campus codes are unique within their School unless a later approved requirement makes them globally unique.
- A Local Node belongs to exactly one Tenant.
- A Local Node may be assigned to multiple Campuses, but every assigned Campus must belong to that same Tenant.
- A Tenant may operate multiple Local Nodes. Node-to-campus assignment is effective-dated so replacement, migration and historical provenance remain traceable.
- V1 does not allow a single Local Node/database to host operational records for multiple tenants.

The proposed cardinality closes the domain ambiguity for implementation planning. Product/legal review must still confirm whether every customer must represent a school group, whether a single legal school may be the tenant, and which legal identifiers are mandatory in each launch jurisdiction.

## 3. Deployment and data ownership

### 3.1 Cloud Control Plane — shared SaaS

SchoolOS operates the Cloud Control Plane. It is a multi-tenant service hosted by SchoolOS and contains:

- Organizations, Schools/Campuses registry metadata and approved contacts.
- Global login identities, tenant memberships and invitations.
- Subscription plans, commercial subscriptions, invoices, payments and entitlement definitions.
- Node registry, node public-key/certificate metadata, heartbeat and revocation state.
- Licence issuance metadata and signed lease/entitlement snapshots.
- Release manifests, rollout channels and update coordination.
- Sync inbox/outbox envelopes, cursors and delivery metadata.
- Support tickets, time-limited support grants and platform audit records.
- Explicitly approved reporting/portal projections.

The Cloud Control Plane does not directly connect to a Local Node's operational database and does not perform direct SQL writes to student, attendance, fee, journal, marks or payroll tables.

### 3.2 Local Data Plane — customer operational boundary

Each Local Node runs the ASP.NET Core modular monolith, React application, PostgreSQL, local identity/authentication, files, background workers and Sync Agent.

- A Local Node database contains data for one Tenant only.
- It may contain one or multiple School/Campus scopes belonging to that Tenant.
- Each operational record retains TenantId and the applicable SchoolId, CampusId and OriginNodeId.
- Local application services enforce role, permission, scope, entitlement, lease and workflow-state checks even while offline.
- A tenant with independent campus outage requirements deploys an independent node for each campus that must continue working without access to another campus node.
- Multiple nodes in one Tenant use explicit ownership and event synchronization. They do not use uncontrolled bidirectional database replication.

### 3.3 Cloud and local tenancy are different enforcement layers

| Layer | Isolation mechanism | Required enforcement |
|---|---|---|
| Cloud API | Authenticated tenant membership and scoped authorization | Every endpoint, query, file, report and job |
| Cloud PostgreSQL | Tenant columns, composite FKs, unique constraints and RLS | Restricted runtime DB role; fail closed without tenant context |
| Local Node | One-tenant database and server-derived tenant context | All Local API/application-service operations |
| Node identity | Tenant-bound certificate/key and node registration | mTLS, signed lease, node status and assigned campus set |
| Sync | Signed tenant/node/event envelope and ownership checks | Validate tenant, node, campus, event type, schema and idempotency |
| Portal/report projections | Explicitly approved tenant-scoped read models | Purpose limitation, minimum fields, freshness and retention |
| Support | Customer-approved, time-limited support grant | Explicit tenant/node/campus scope, audit and automatic expiry |

No single control replaces the others. UI filtering is never a security boundary.

## 4. Global identity and tenant membership

### 4.1 Identity model

A person has one globally unique SchoolOS UserId. The person may have membership in one or more customer tenants, but membership is explicit and independently revocable.

Core entities:

| Entity | Scope | Purpose |
|---|---|---|
| Users | Global identity | Stable person/login identity and account status |
| UserCredentials / ExternalIdentityLinks | Global | Credential/provider links; secrets protected separately |
| TenantMemberships | Tenant | User's relationship with an organization and membership status |
| TenantInvitations | Tenant | Invitation, expiry, acceptance and revocation |
| MembershipRoleAssignments | Tenant/school/campus | Role grants with explicit effective scope |
| PermissionDefinitions | Global catalogue | Stable permission keys |
| RolePermissionAssignments | Role catalogue / tenant-custom role | Permission bundle |
| ResourceAssignments | Tenant and resource | Teacher/class/subject, guardian/student and other relationship scope |
| UserSessions / RefreshSessions | Audience-specific | Session lifecycle, revocation and device metadata |
| MfaFactors | Global identity | MFA enrollment and recovery metadata |

Do not use the User table itself as proof of tenant access. A user with an account but no active TenantMembership cannot access tenant data.

### 4.2 Membership rules

- A membership has a TenantId, UserId, status, activation date, optional end date and audit fields.
- Enforce a unique active membership per (TenantId, UserId) unless a specific multi-profile requirement is approved.
- Tenant owners invite users by verified email or an approved invitation channel. The invitation is bound to one tenant, has a short expiry and can be accepted only once.
- Membership removal revokes new sessions and triggers the Local Node membership projection update on next synchronization.
- A user who belongs to multiple tenants must actively select the intended tenant context. Do not merge roles across tenants.
- A user cannot invite themselves into an arbitrary tenant by changing an organization ID in a request.
- Platform staff identities and roles are separate from customer memberships. A PlatformSuperAdmin role is not a tenant role and is not silently inherited by tenant APIs.
- A parent/guardian relationship is a resource relationship, not a substitute for membership or student-record authorization.

### 4.3 Authentication audiences

Keep separate authentication audiences and session boundaries for:

1. Public/anonymous registration and website.
2. Cloud customer portal.
3. SchoolOS internal Control Plane administration.
4. Local school staff application.
5. Parent/student remote portal, if enabled.
6. Demo environments.
7. Customer-approved support sessions.
8. Local Node service-to-service identity.

A valid token for one audience must not be accepted by another. In particular, demo and customer portal tokens cannot call platform administration endpoints.

### 4.4 Offline local identity

Local login cannot depend on a live Cloud authentication round trip.

- The Cloud may be the master for organization-level membership administration, but every Local Node maintains a validated local projection of the users and membership/role/scope grants required for its assigned campuses.
- A local user authenticates against the Local Node's supported local identity mechanism. Local access tokens/cookies have a Local Node issuer and Local API audience and are not Cloud portal credentials.
- Local session, password reset, MFA and recovery policy must be documented for the pilot. A tenant admin cannot weaken platform minimum security.
- Revocations and role changes synchronize to nodes. The system records last-known membership version and last sync time.
- During a Cloud outage, already synchronized local users continue within the documented offline policy. A new user or a material privilege escalation cannot be activated on an offline node unless a separately approved signed administrative package supports it.
- When connectivity returns, revoked users/devices and changed grants are reconciled before the next privileged operation where practical. Offline revocation delay is a documented residual risk, not something the platform claims to eliminate.

## 5. Tenant context resolution on every request

Tenant context must be resolved by trusted server-side code after authentication and before protected application handlers access tenant-owned data.

### 5.1 Request pipeline

Recommended API pipeline:

1. Route selection and request-size/rate controls.
2. Authentication: validate signature, issuer, audience, expiry and session/device status.
3. Resolve tenant candidate from the authenticated tenant-bound session/token and/or the tenant slug/host.
4. Cross-check that the resolved tenant is active and the authenticated user has an active membership or a specific authorized platform/support grant.
5. Resolve effective school/campus/resource assignments.
6. Create an immutable request-scoped TenantContext.
7. Begin the database unit of work and set database tenant context transaction-locally.
8. Run authorization: permission + scope + resource relationship + state + entitlement/lease where relevant.
9. Execute the application operation and audit it.
10. Dispose/commit/rollback the transaction and tenant context.

The middleware order and exact ASP.NET Core registration must be captured in the implementation ADR and integration tests.

### 5.2 Trusted and untrusted tenant selectors

Acceptable tenant resolution inputs:

- A tenant-bound access token created after membership validation.
- A verified tenant hostname/subdomain mapped to the canonical TenantId, followed by membership validation.
- An explicit tenant selection made by the user, exchanged server-side for a tenant-bound session after membership validation.
- A platform/support grant with explicit scope and expiry.

Never treat the following as authorization proof:

- X-Tenant-Id or another caller-controlled header.
- tenantId in a request body/query string.
- A tenant slug or organization name by itself.
- A frontend dropdown selection.
- A resource ID without checking that the resource belongs to the resolved tenant.

If the route/host tenant and authenticated tenant claim disagree, reject the request. Do not silently switch tenant context. Do not disclose whether another tenant's resource exists.

### 5.3 TenantContext contract

The immutable server-side context should carry at least:

- TenantId (canonical organization UUID).
- UserId or service principal ID.
- Authentication audience and session/device ID.
- Membership ID and membership version.
- Effective permission set or permission resolver reference.
- Allowed SchoolIds and CampusIds or an efficient scoped authorization resolver.
- Optional support-grant ID and purpose.
- CorrelationId and request origin.
- NodeId and licence/entitlement version for Local Data Plane requests.

Do not populate it directly from a DTO. Background workers and sync handlers must construct an equivalent validated context from a durable job/event envelope and service identity.

### 5.4 Fail-closed behavior

- Missing, malformed, inactive, ambiguous or mismatched tenant context returns a controlled authorization error.
- A tenant-owned query must not execute before context establishment.
- A tenant-scoped database connection without transaction-local tenant context must see zero tenant-owned rows and must reject writes.
- An invalid tenant must not fall back to a global/default tenant.
- Platform-wide endpoints use a distinct, explicitly authorized path and service; they must not disable tenant filters in ordinary customer handlers.
- Support access is a separate, auditable authorization grant, not a hidden super-admin bypass.

## 6. Cloud database design and constraints

### 6.1 Shared database baseline

V1 uses one Cloud PostgreSQL cluster/database with module-owned schemas and shared tenant-aware tables. This is a deliberate SaaS operating-cost and deployment choice, not a claim that one shared database is the only possible design.

Every tenant-owned Cloud table must have a non-null TenantId/OrganizationId value directly or through a parent relationship that is enforced by composite foreign keys and a reviewed query path. Prefer a direct tenant column on high-volume/security-sensitive tables where it makes isolation and indexing easier.

Global catalogue tables (for example permission definitions, plan definitions and public release metadata) are explicitly classified as platform-global. They are not made tenant-owned by adding nullable TenantId fields. Maintain a reviewed allowlist of truly global tables.

### 6.2 Minimum Cloud schema entities

The detailed ERD remains in DOC-36 and DOC-44. The minimum SaaS tenancy entities are:

| Schema/table | Key fields and constraints |
|---|---|
| organizations.Organizations | Id, Slug, LegalName, DisplayName, Status, CreatedAtUtc; unique normalized Slug |
| organizations.Schools | Id, TenantId, LegalName, SchoolCode, Status; unique (TenantId, SchoolCode); unique (TenantId, Id) |
| organizations.Campuses | Id, TenantId, SchoolId, CampusCode, Name, Status; unique (TenantId, Id); FK (TenantId, SchoolId) to Schools |
| organizations.LocalNodes | Id, TenantId, NodePublicKeyThumbprint, Status, EnrolledAtUtc, RevokedAtUtc; unique (TenantId, Id) |
| organizations.NodeCampusAssignments | TenantId, NodeId, CampusId, EffectiveFromUtc, EffectiveToUtc; composite FKs ensure same-tenant assignment |
| identity.Users | Id, normalized login identifiers, account status, security stamp; global unique constraints on verified identity subject |
| identity.TenantMemberships | Id, TenantId, UserId, Status, MembershipVersion; unique (TenantId, UserId) |
| identity.TenantInvitations | Id, TenantId, normalized invitee, role/scope proposal, token hash, ExpiresAtUtc, AcceptedAtUtc |
| identity.MembershipRoleAssignments | TenantId, MembershipId, RoleId, ScopeType, ScopeId, EffectiveFromUtc, EffectiveToUtc |
| subscriptions.OrganizationSubscriptions | Id, TenantId, PlanId, Status, EffectiveFromUtc, EffectiveToUtc |
| subscriptions.EntitlementAssignments | TenantId, EntitlementKey, SourceSubscriptionId, Value/Limit, EffectiveFromUtc, EffectiveToUtc |
| licensing.LicenseLeases | Id, TenantId, NodeId, LeaseSequence, IssuedAtUtc, NotBeforeUtc, ExpiresAtUtc, SigningKeyId, PayloadDigest, RevokedAtUtc |
| audit.AuditLogs | TenantId where tenant-related, ActorId, Action, ResourceType/Id, Result, Reason, OccurredAtUtc, CorrelationId |

Table names are conceptual and must be reconciled with the existing module-owned schema catalogue before migrations. Do not create duplicate entities where an existing equivalent already exists.

### 6.3 Composite tenant constraints

Use database constraints to prevent cross-tenant references even if an application bug supplies a foreign ID.

Illustrative pattern:

    ALTER TABLE organizations.schools
      ADD CONSTRAINT uq_schools_tenant_id UNIQUE (tenant_id, id);

    ALTER TABLE organizations.campuses
      ADD CONSTRAINT fk_campuses_school_same_tenant
      FOREIGN KEY (tenant_id, school_id)
      REFERENCES organizations.schools (tenant_id, id);

    ALTER TABLE organizations.campuses
      ADD CONSTRAINT uq_campuses_tenant_id UNIQUE (tenant_id, id);

    ALTER TABLE organizations.node_campus_assignments
      ADD CONSTRAINT fk_assignment_node_same_tenant
      FOREIGN KEY (tenant_id, node_id)
      REFERENCES organizations.local_nodes (tenant_id, id);

    ALTER TABLE organizations.node_campus_assignments
      ADD CONSTRAINT fk_assignment_campus_same_tenant
      FOREIGN KEY (tenant_id, campus_id)
      REFERENCES organizations.campuses (tenant_id, id);

Use the same pattern for any child entity that references a parent in a different tenant-owned table. Add indexes beginning with TenantId for common scoped lookup patterns. Never rely only on globally unique UUIDs to prevent an IDOR or cross-tenant relation.

### 6.4 Application data access

- Introduce a single TenantContext abstraction and a shared tenant-aware data-access component in the existing BuildingBlocks layer.
- EF Core global query filters may provide a default TenantId filter, but they are defense in depth only; they are not a substitute for explicit authorization or RLS.
- Make the tenant column immutable after insert. Tenant transfer is a controlled migration workflow, not an ordinary update.
- SaveChanges validation rejects missing or mismatched TenantId values for tenant-owned entities.
- All queries used by list, search, dashboard counts, pagination, aggregate reports, file lookup, export and background processing must apply the same scope.
- Avoid unrestricted repository methods such as GetById(id) for tenant-owned entities. Provide tenant-scoped methods such as GetById(tenantId, id) or aggregate access through TenantContext.
- Cache keys include TenantId and any other required scope. Cache invalidation and distributed locks are tenant-scoped.
- Never include student/financial payloads or secrets in logs, metrics labels or trace baggage.

## 7. PostgreSQL RLS and connection-pool safety

RLS is defense in depth for the shared Cloud Control Plane database. Application authorization and tenant-aware query design remain mandatory.

### 7.1 Runtime database roles

Use separate database roles:

- Schema/migration owner: used only by controlled migration tooling; never by web/API runtime.
- Cloud application runtime role: restricted, not superuser, not table owner, and without BYPASSRLS.
- Background worker role: similarly restricted; tenant context is derived and validated per job.
- Read-only reporting role: restricted to approved reporting views/projections and tenant policy.
- Break-glass operator: separately controlled, just-in-time, dual-approved where appropriate, logged and not stored in application configuration.

The normal Local Node application role is also non-superuser and must not own operational tables. Do not place migration-owner credentials in application environment variables.

### 7.2 Transaction-local tenant context

For every tenant-scoped database unit of work:

1. Begin a database transaction.
2. Set the tenant context using a transaction-local setting (for example, set_config('app.tenant_id', <validated UUID>, true) or SET LOCAL).
3. Execute all tenant-scoped reads and writes inside that transaction.
4. Commit or roll back; transaction end clears the setting before the pooled connection is reused.

Do not set tenant context at session scope on a pooled connection. Do not run tenant-scoped SQL before the setting is established. Background jobs, sync consumers and scheduled tasks must set context from their validated durable envelope, not from process-global state.

### 7.3 Policy pattern

Illustrative SQL only; final policy names and coverage are defined in DOC-44 and reviewed in the security ADR.

    ALTER TABLE students.student_profiles ENABLE ROW LEVEL SECURITY;
    ALTER TABLE students.student_profiles FORCE ROW LEVEL SECURITY;

    CREATE POLICY student_tenant_scope
    ON students.student_profiles
    USING (
      tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
    )
    WITH CHECK (
      tenant_id = nullif(current_setting('app.tenant_id', true), '')::uuid
    );

When tenant context is absent, the comparison does not match a tenant row and access fails closed. Verify this behavior against every table and operation. Review superuser/table-owner bypass behavior; FORCE ROW LEVEL SECURITY does not make a superuser safe.

### 7.4 RLS rollout gate

- Inventory every table as platform-global, tenant-owned, or node-local single-tenant.
- Prove that every tenant-owned table has correct RLS and tenant constraints.
- Verify select, insert, update, delete, returning clauses, joins, views, functions and reporting queries.
- Test cross-tenant and missing-context behavior using the real restricted runtime role.
- Test pooled connection reuse across sequential requests for different tenants.
- Test transaction rollback/exception paths and background worker execution.
- Ensure migration tooling can operate without granting the runtime role RLS bypass.
- Do not enable the production tenant service until automated RLS coverage and independent security review pass.

## 8. Local Data Plane isolation

A Local Node is provisioned for exactly one Tenant. Its database is not a shared multi-customer database in v1.

- The TenantId is pinned in protected node configuration at enrollment and is also verified against the signed node certificate/lease.
- Every operational entity retains TenantId, even where the database physically contains only one tenant.
- Local application startup checks the database TenantId, NodeId, tenant-bound certificate and signed lease; a mismatch fails readiness.
- The Local Node cannot change its tenant by editing an environment variable or request header after enrollment. Rebinding requires a controlled decommission/re-enrollment and data migration with backup, audit and customer approval.
- School/Campus authorization is still enforced. Being on a tenant's node does not grant every staff member access to every campus.
- If one node serves several campuses of the same tenant, node assignment does not merge campus authorization; each user's campus grant still applies.
- A second node for the same tenant receives its own identity, certificate and assignment list. Cross-node business events retain origin node and campus.
- PostgreSQL is never directly exposed to the public internet.

## 9. Tenant lifecycle and provisioning

### 9.1 Separate onboarding status from subscription status

Do not overload one Status column to represent registration, provisioning, billing and service health.

Recommended Organization lifecycle:

    PendingVerification
      -> PendingReview
      -> Approved
      -> Provisioning
      -> Trial or Active
      -> Suspended
      -> Closing
      -> Closed

Actual transitions are controlled commands with actor, reason, timestamp and audit. A rejected registration is retained in a separate application/review state and is not an active tenant.

The subscription has its own state machine (for example Draft, Trialing, Active, PastDue, Suspended, Cancelled, Expired). Node health and sync status are independent operational states.

### 9.2 New tenant onboarding workflow

1. **Registration:** public visitor submits organization details, legal school details, primary owner contact, country/timezone, expected campus count and requested deployment profile.
2. **Verification:** verify email/phone and anti-abuse controls. Do not expose an active tenant based only on an unverified email.
3. **Review:** apply product/commercial review rules; capture terms/privacy acceptance and required legal evidence.
4. **Create tenant:** create Organization with immutable UUID and normalized unique slug; create its initial School and Campus records; create the owner User and Pending/Active TenantMembership according to verified workflow.
5. **Provision subscription:** create trial/paid subscription and effective-dated entitlement snapshot from an approved plan; do not assume that a registration form grants every module.
6. **Provision customer portal:** create portal membership, initial owner access, notification preferences and required security enrollment.
7. **Prepare Local Node enrollment:** create a short-lived, single-use enrollment challenge bound to TenantId and intended node/site; store only a hash of the secret.
8. **Install and enroll node:** Local Node generates its key pair locally and proves possession during bootstrap over TLS. Cloud validates the challenge, tenant, expected deployment and software version before issuing a node certificate.
9. **Issue lease:** Cloud License Service issues a signed, node-bound lease containing the approved tenant, node, entitlement, sequence and validity claims.
10. **Initialize local operations:** apply signed release/migrations, bind TenantId/NodeId, seed approved reference data, initialize local administrator and run isolation smoke tests.
11. **Handover:** record node-to-campus assignments, backup/restore evidence, service contacts, outage behavior, licence renewal and customer acceptance.

Provisioning is a durable workflow/saga, not one long HTTP transaction. Every step is idempotent and resumable. Failure records the failed step and compensation/retry action; it must not create duplicate tenants, memberships, subscriptions, nodes or invoices.

### 9.3 Tenant status behavior

| Tenant state | Customer portal | Local operations | New node enrollment |
|---|---|---|---|
| PendingVerification / PendingReview | Restricted registration/review only | Not activated | Denied |
| Approved / Provisioning | Owner setup and provisioning status | Not yet generally enabled | Allowed only for approved enrollment |
| Trial / Active | Normal subscribed features | Allowed under valid lease | Allowed by node/plan limits |
| Suspended | Billing/support/data export according to policy | Existing signed lease and Restricted Mode policy apply; do not invent an immediate offline kill switch | Denied unless reactivation workflow |
| Closing | Export, renewal/exit and support only | Customer-agreed read/export period; new writes follow approved policy | Denied |
| Closed | No normal login; controlled legal/export access only | Decommissioned after approved export/retention | Denied |

The exact grace period, expiry operation matrix, customer data retention and cancellation policy remain governed by DEC-010, DEC-017, DEC-018 and DOC-46. A Cloud subscription status change cannot instantly disable an offline node that has not received a signed revocation; the offline lease boundary is the enforceable limit.

## 10. Local Node enrollment and device identity

### 10.1 Enrollment security

- Enrollment challenge is high entropy, single-use, tenant-bound, node-purpose-bound and short-lived.
- Persist only a cryptographic hash of the challenge; show the bootstrap secret once.
- Local Node generates a private key locally; the private key never leaves the node.
- Cloud validates proof of possession and issues a certificate/credential bound to TenantId and NodeId.
- Use outbound mTLS for normal node-to-cloud traffic; no public inbound port is required on the Local Node.
- Node identity is not a human login and cannot call arbitrary customer or platform endpoints.
- The Cloud registry stores certificate thumbprint/public-key metadata, lifecycle, last heartbeat, software version and assigned campuses, not private keys.
- Certificate renewal, key rotation, deactivation and revocation are audited.
- Re-enrollment to a different tenant requires explicit decommissioning of the old binding and a reviewed data migration; a node cannot self-serve tenant reassignment.

### 10.2 Node authorization

Every node request validates:

1. Certificate/proof-of-possession and credential status.
2. TenantId and NodeId binding.
3. Requested endpoint audience and operation.
4. Assigned CampusId set and allowed event types.
5. Lease signature, validity, sequence and revocation policy.
6. Schema/software compatibility.
7. Replay/idempotency and rate limits.

A valid node certificate is not a user permission grant. A signed lease is not a replacement for user authorization.

## 11. Subscription, entitlement and licence enforcement

### 11.1 Separate commercial and operational gates

For a protected capability, the Local Data Plane evaluates all applicable gates:

1. Module is installed and technically compatible.
2. Tenant subscription has an effective entitlement.
3. Node's signed lease contains the relevant entitlement/limit.
4. User has the required permission.
5. User has correct School/Campus/resource scope.
6. Tenant, node and licence states allow the operation.
7. Workflow state and Restricted Mode policy allow the operation.

All checks run server-side. Hiding navigation items is only a UX behavior.

### 11.2 Entitlement snapshot

Cloud is authoritative for plan/catalogue/subscription status. It publishes a versioned, signed entitlement snapshot to the Tenant's registered nodes.

Minimum claims:

- TenantId and NodeId.
- SubscriptionId and PlanId (or opaque references).
- Entitlement keys and values/limits.
- Effective-from and effective-to times.
- Lease sequence and previous sequence reference.
- Catalogue/schema compatibility version.
- IssuedAtUtc, NotBeforeUtc and ExpiresAtUtc.
- Signing key identifier and signature.
- Optional grace/restricted-mode claims only after DEC-010 approval.

A local verifier checks the signature using a pinned trusted public key, tenant/node match, sequence monotonicity, validity window and supported catalogue version. Never embed the signing private key in the Local Node. Do not use the machine clock alone as a trusted time source without the clock-rollback policy.

### 11.3 Billing and expiry

- Invoices/payments determine subscription state through verified Cloud billing workflows.
- Browser redirects or unverified webhook payloads do not activate entitlements.
- Entitlement changes are versioned and audited.
- If the Cloud is unavailable, the Local Node uses its last valid signed lease until the signed policy boundary; it does not need a live Cloud request for every feature check.
- At lease/grace expiry, the Local Node applies the approved Restricted Mode operation matrix. Preserve customer data; do not delete student, finance or historical records because a subscription lapsed.
- A node's local display must show last successful entitlement synchronization and expiry status.

Final plan packaging, module dependency, capacity limits and lease duration are DEC-020 decisions. Exact grace and Restricted Mode permissions are DEC-010 decisions.

## 12. Tenant-aware synchronization and reporting

- Every event envelope includes TenantId, SchoolId where applicable, CampusId where applicable, OriginNodeId, EventId, AggregateId/version, schema version, occurrence time and correlation ID.
- Cloud authenticates the node and verifies that TenantId/NodeId/CampusId assignment agrees with the registry.
- Receiving nodes reject cross-tenant messages, unassigned-campus events, unsupported schemas, replayed operation IDs and invalid event ownership.
- Event payloads contain only minimum data required for the event. Avoid child identity or financial payloads in routing metadata.
- Delivery is at least once. Handlers enforce idempotent business effects.
- Cloud never treats the sync envelope's tenant field alone as proof; it compares it to the authenticated node identity.
- Cloud reporting/portal projection is a separate approved read model with purpose, field list, freshness label, retention and tenant access policy.
- Tenant data exports include only that tenant's own records and relationships. Cross-tenant analytics must be explicitly approved, aggregated/de-identified where appropriate and access-controlled.

The Cloud Sync Hub does not write directly into a Local Node database. The Local Node applies validated commands/events through its application services and ownership rules.

## 13. Authorization, support and platform operations

DOC-46 remains the role/permission matrix. This section makes its tenancy enforcement executable:

- Every tenant endpoint resolves TenantContext and uses a scope-aware application service.
- Every list and count is scoped before filtering, aggregation, pagination and totals are calculated.
- Every detail/read path checks tenant ownership before loading sensitive fields.
- File download, object-store keys, import packages, export jobs, background emails and notifications carry TenantId and revalidate authorization at execution time.
- Tenant roles are granted by the tenant's authorized owner/admin within the permitted delegation boundary. Customer roles cannot grant Platform permissions.
- Platform support access requires a separately approved SupportAccessGrant containing named agent, TenantId, NodeId/CampusId scope, purpose/ticket, start, expiry and access level.
- Support is read-only by default. Elevated action needs separate approval, step-up authentication and visible audit.
- Platform operations do not receive routine access to student, guardian, marks or finance payloads merely because they operate the SaaS service.
- Break-glass access is time-limited, reason-coded, separately approved and post-reviewed.
- Tenant offboarding includes authenticated export, node deactivation/revocation, retention/legal hold, backup disposition and deletion/anonymization evidence.

## 14. API and event contract outline

DOC-45 remains the authority for final route naming, versioning, DTOs and OpenAPI approval. The following endpoint families are the v1 functional contract; exact route shapes may be adapted without weakening the invariants.

| API family | Example operation | Required authorization |
|---|---|---|
| Public registration | Submit organization registration | Anonymous, validated/rate-limited; creates review application only |
| Tenant context | List memberships / select tenant context | Authenticated identity; only active memberships returned |
| Tenant administration | Read/update tenant profile, schools and campuses | Tenant membership + scoped organization permissions |
| Invitations | Invite, resend, accept/revoke invitation | Tenant admin; recipient verification; single-use token |
| Subscription | Read plan, subscription, invoice and entitlement status | Tenant membership and commercial permission |
| Node enrollment | Create enrollment challenge; bootstrap node | Authorized tenant/platform workflow; single-use tenant-bound challenge |
| Node management | Heartbeat, certificate renewal, node status, campus assignment | Authenticated node identity or authorized tenant/platform operator |
| Licence | Issue/renew/revoke signed lease | Protected License Service; audited privileged workflow |
| Sync | Upload events, fetch commands, acknowledge cursor | mTLS node identity; tenant/node/campus ownership validation |
| Support | Request/approve/revoke support grant | Customer approver and scoped platform support identity |
| Export/offboarding | Prepare tenant export and close tenant | Authorized tenant owner; policy/legal checks; auditable job |

All operations that create or update tenant-owned resources take TenantId from TenantContext, not from a mutable request DTO. When the client supplies a target school/campus/resource ID, the server proves it belongs to the active tenant and permitted scope.

Standard failures should distinguish (without leaking existence):

- Unauthenticated/invalid audience.
- Tenant context missing or invalid.
- Membership inactive or revoked.
- Permission/scope denied.
- Tenant/node suspended or not provisioned.
- Entitlement or lease unavailable/expired.
- Campus not assigned to node.
- Duplicate/replayed idempotency key.
- Incompatible event/schema version.

Use the stable API error conventions in DOC-45.

## 15. Audit, observability and privacy

Audit, logs and metrics must preserve security context without leaking personal/financial data.

Audit at minimum:

- Tenant create/approve/suspend/close and hierarchy changes.
- Membership invitation, acceptance, role/scope change and revocation.
- Subscription/entitlement change and invoice/payment verification.
- Node enrollment, certificate rotation/revocation, campus assignment and lease issuance.
- Support grant request/approval/use/expiry.
- Cross-tenant access denial, invalid tenant context and RLS rejection where safely observable.
- Export, offboarding, restore and break-glass actions.

Each tenant-related audit record includes TenantId, ActorId/ServiceId, Action, ResourceType/Id, Result, Reason where required, OccurredAtUtc, CorrelationId and OriginNodeId where applicable.

Operational logs and metrics must not contain student names, guardian phone numbers, national IDs, payment credentials, tokens, passwords, node private keys or lease signing secrets. Do not create unbounded per-tenant metric labels. Provide tenant-scoped dashboards through protected query paths; platform-wide dashboards use aggregated operational metadata unless a support grant permits more.

## 16. SaaS tenant acceptance tests

Add these scenarios to the canonical task/test traceability workflow. Existing SEC-001, SEC-005, LIC-001–004 and HYB-001–003 remain required and are not replaced.

### SAAS-001 — Registration provisions one tenant idempotently (P0)

**Given** a verified, approved registration request with a stable idempotency key.  
**When** the same provisioning command is retried after a timeout or worker restart.  
**Then** exactly one Organization, initial School, Campus, owner membership and trial/subscription record exist; the operation resumes or returns the existing result.

### SAAS-002 — Organization hierarchy cannot cross tenants (P0)

**Given** Tenant A and Tenant B, each with Schools and Campuses.  
**When** a request or SQL write attempts to attach Tenant A's Campus to Tenant B's School or Node.  
**Then** composite foreign keys and application validation reject the relationship and audit the denied attempt.

### SAAS-003 — Tenant switch requires an active membership (P0)

**Given** a user belongs to Tenant A but not Tenant B.  
**When** the user submits a tenant switch or changes the tenant route/slug to Tenant B.  
**Then** no tenant-bound session is issued for Tenant B and no Tenant B metadata or records are disclosed.

### SAAS-004 — Route/host and authenticated tenant mismatch is denied (P0)

**Given** a valid Tenant A session.  
**When** the caller changes the route or host to Tenant B.  
**Then** the request is denied before tenant-owned data is read; the system does not silently replace the session tenant.

### SAAS-005 — Missing tenant context fails closed (P0)

**Given** the Cloud runtime database role and a tenant-owned table.  
**When** a query or write executes without transaction-local tenant context.  
**Then** no tenant rows are readable and writes are rejected; there is no fallback tenant.

### SAAS-006 — RLS and connection-pool context do not leak (P0)

**Given** pooled database connections used sequentially for Tenant A and Tenant B.  
**When** one request commits, rolls back or throws before the next tenant request uses the pool.  
**Then** the next transaction sees only its own tenant rows; no prior context survives.

### SAAS-007 — Tenant-owned foreign keys are tenant-consistent (P0)

**Given** globally unique object IDs from two tenants.  
**When** an API or direct persistence attempt creates a cross-tenant parent/child reference.  
**Then** application validation and database composite constraints reject the reference.

### SAAS-008 — Node enrollment is tenant-bound and single-use (P0)

**Given** an enrollment challenge for Tenant A.  
**When** it is expired, replayed, used without proof of the registered node key, or presented for Tenant B.  
**Then** enrollment is denied and no valid node credential/lease is issued.

### SAAS-009 — A Local Node cannot host a second tenant (P0)

**Given** a node already enrolled for Tenant A.  
**When** an administrator, configuration change or request attempts to rebind it to Tenant B without decommission/re-enrollment.  
**Then** startup/readiness or enrollment validation fails and Tenant A's data is not exposed or rewritten.

### SAAS-010 — Entitlement and user authorization are independent (P0)

**Given** a tenant has an active module entitlement but a user lacks its permission, or a user has the permission but the tenant lacks the entitlement.  
**When** the module API is called.  
**Then** access is denied in either case; the UI is not the enforcement point.

### SAAS-011 — Local operations survive Cloud outage within valid lease (P0)

**Given** an enrolled Local Node with a valid signed lease and synchronized local user membership.  
**When** internet/Cloud connectivity is lost.  
**Then** approved local operations and local authentication continue without a Cloud call; sync queues locally and lease policy is enforced.

### SAAS-012 — Sync rejects wrong-tenant or wrong-campus node events (P0)

**Given** a node certificate bound to Tenant A and assigned Campus A1.  
**When** it submits an event claiming Tenant B or an unassigned Campus B1.  
**Then** Cloud rejects the event before routing or projecting its payload and records a security event.

### SAAS-013 — Tenant export and offboarding remain tenant-scoped (P1)

**Given** a tenant owner requests an authorized data export and later tenant closure.  
**When** export and decommission workflows execute.  
**Then** the package contains only the tenant's authorized data and relationships, node credentials are revoked, retention/legal-hold rules are applied, and each stage has auditable evidence.

### SAAS-014 — Platform support cannot become permanent tenant access (P0)

**Given** a customer-approved, time-limited read-only support grant.  
**When** the grant expires or is revoked.  
**Then** all support sessions/tokens lose access; the agent cannot continue using a cached or generic platform role to read tenant records.

## 17. Implementation sequence and delivery gates

Implement one vertical slice at a time. Do not attempt to enable all modules or migrate all customers before the tenant boundary is proven.

| Phase | Deliverable | Exit gate |
|---|---|---|
| 0. Decision and threat-model approval | Approve DEC-001, DEC-004, DEC-008 and tenancy implications of DEC-020; approve data-flow/threat model | Signed ADRs and reviewed ERD/API contract |
| 1. Identity and tenant foundation | TenantContext, Organization/School/Campus, User/TenantMembership, membership selection and invitation foundations | SAAS-002–004 pass; default-deny authorization established |
| 2. Cloud persistence isolation | Tenant-aware EF/data access, composite FKs, RLS, DB roles, pooled transaction context | SAAS-005–007 and SEC-001/005 pass using restricted runtime role |
| 3. Registration/provisioning | Verified registration, approval, idempotent provisioning saga, tenant status and audit | SAAS-001 passes across retries/failure recovery |
| 4. Tenant admin and subscriptions | Tenant portal, tenant role management, plans, subscription states, entitlement catalogue | SAAS-010 and billing/entitlement acceptance tests pass |
| 5. Local Node enrollment | One-time challenge, node key pair, mTLS identity, node-campus assignment, signed lease | SAAS-008–009 and LIC tests pass |
| 6. Offline identity and sync | Local membership projection, local login, tenant-bound sync, event replay protection | SAAS-011–012 and offline/sync tests pass |
| 7. First operational module | One vertical business workflow (recommended: organization setup + admissions or attendance) through API, DB, authorization, audit and offline path | End-to-end tenant isolation and business acceptance evidence |
| 8. Production readiness | Restore drill, export/offboarding, monitoring, incident/support policy, independent security review | All P0 tests pass; operational/legal/commercial gates approved |

The first production pilot must not begin until the sign-off gates in DOC-51 are satisfied and all relevant P0 SaaS tenant tests are recorded as passed with evidence.

## 18. Definition of Done — real SaaS tenant foundation

The tenant foundation may be called implemented only when all of the following are true:

- Tenant, School and Campus migrations exist with approved keys, constraints and indexes.
- Cloud registration creates a tenant through a retry-safe provisioning workflow.
- A verified user can hold separate memberships in multiple tenants and switch only to an active membership.
- Every tenant API, background worker, export, file path, report and sync handler receives validated tenant scope.
- Cross-tenant reads/writes and cross-tenant relationships are rejected by application authorization and database safeguards.
- Cloud PostgreSQL runtime credentials cannot bypass RLS; RLS and connection-pool isolation tests pass.
- Each Local Node is cryptographically bound to exactly one tenant and its assigned campuses.
- A valid signed node lease and locally synchronized membership allow approved operations through Cloud outages.
- Subscription entitlements and user permissions are checked separately on the server.
- Audit, monitoring, backup/restore, tenant export, offboarding and support-access controls are tested.
- All P0 acceptance tests in this document, DOC-44, DOC-46 and DOC-47 pass in CI or a reproducible test environment.
- A pilot customer completes a documented restore, outage and cross-tenant isolation exercise.
- The release notes and product claims accurately distinguish implemented capabilities from planned capabilities.

A UI tenant switcher, a TenantId column, an EF query filter, a successful login, or a passing unit test alone does not satisfy this definition.

## 19. Decision and document traceability

| Decision / specification | DOC-62 treatment | Required follow-up |
|---|---|---|
| DEC-001 — Tenant/School/Campus cardinality | Proposes Organization/Tenant → School → Campus; node binds to one tenant and one/many same-tenant campuses | Product/legal approval and ERD closure |
| DEC-004 — RLS context, DB roles and pooling | Specifies transaction-local app.tenant_id, restricted runtime role, FORCE RLS, fail-closed behavior and pool tests | Security review and executable integration tests |
| DEC-008 — Authentication and local identity | Specifies separate audiences, global identity/membership, Local Node identity projection | Select provider, MFA/recovery and local credential lifecycle |
| DEC-020 — Catalogue and signed entitlement | Specifies separate subscription entitlement, user permission and node-bound signed lease gates | Approve core/paid catalogue, limits, compatibility and lease claims |
| DEC-010 — Restricted Mode | Preserves operation-level approval requirement | Approve exact allow/deny operation matrix |
| DOC-14 | Hybrid Cloud/Local deployment boundary | Add this tenant implementation contract to deployment design |
| DOC-36 / DOC-44 | Cloud data model, ERD, DB constraints and RLS | Reconcile final table names/keys and create migrations |
| DOC-45 | APIs, identity audience, sync envelope and errors | Publish OpenAPI contracts and contract tests |
| DOC-46 / DOC-47 | Permission matrix and acceptance tests | Add SAAS test IDs to task/test traceability |
| DOC-48 / DOC-51 | Provisioning/operations and release sign-off | Build runbooks and retain pilot/security evidence |

## 20. Explicit non-goals and safeguards

V1 does not:

- Put every student's operational record in the Cloud Control Plane.
- Share one Local Node database among unrelated customer tenants.
- Trust frontend tenant selectors, route IDs, user-submitted TenantIds or headers.
- Treat a hidden menu item, global EF query filter or UUID secrecy as tenant security.
- Let subscription entitlement grant a user permission or let a role bypass a missing subscription.
- Let Cloud services directly mutate local operational tables.
- Promise instant remote revocation of a disconnected Local Node.
- Claim exactly-once network delivery; business effects are made idempotent.
- Mark architecture decisions approved without owner review.
- Claim SaaS tenant code is implemented until the Definition of Done and test evidence are satisfied.

This document is the v1 implementation baseline proposal for the SchoolOS SaaS tenant foundation. Update it through reviewed ADRs and linked evidence as implementation decisions are approved.
