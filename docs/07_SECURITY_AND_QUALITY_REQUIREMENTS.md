# 07 — Security, Privacy and Non-Functional Requirements

- **Document ID:** DOC-07
- **Status:** Current
- **Decision maturity:** Proposed security and quality baseline
- **Owner:** Technical lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ 898812f89e0ec7f03aff6c3b1408e46782814dfd; reference sources pinned in DOC-61

## 1. Security baseline

- Authentication through ASP.NET Core Identity or a reviewed identity provider.
- Multi-factor authentication for privileged roles where practical.
- Policy-based authorization with tenant/school/campus scope.
- Server-side scope derivation; never trust client-supplied tenant IDs.
- Input validation and output encoding.
- API rate limiting and abuse controls.
- Secure session/token lifecycle and revocation.
- Secrets stored outside source control.
- TLS for network traffic.
- Encryption at rest for database backups, storage and supported infrastructure.
- Private document storage and short-lived authorized download links.
- Audit logs for protected changes and data access events as appropriate.
- Security patching and dependency scanning.
- Tested backups and restore procedures.

## 2. Tenant isolation

Use multiple layers:

1. Application authorization and scoped query services.
2. Module-level tenant filters/guards.
3. PostgreSQL RLS where suitable.
4. Restricted database roles and careful owner/bypass configuration.
5. Automated cross-tenant access tests for every important entity and endpoint.

If connection pooling is used with session-based RLS context, set context transaction-locally and test that no tenant context leaks across pooled connections. Do not assume RLS alone solves every authorization issue.

## 3. Sensitive data and minors

The system may hold children's personal, guardian, attendance, academic and document data. Requirements:

- Collect only data needed for a stated purpose.
- Define lawful basis/consent and guardian workflows with qualified legal advice.
- Restrict biometric data; face recognition is not part of the initial scope.
- Define retention, archival, correction and deletion policies.
- Protect exports and bulk downloads.
- Log privileged access and sensitive changes.
- Review third-party AI/provider processing terms, retention and model-training use.
- Establish incident response and breach escalation procedures.
- Obtain a Pakistan-specific legal review before production, including applicable privacy, cyber, education and child-data obligations.

This document is not legal advice.

## 4. Finance controls

- Double-entry ledger.
- Balanced journal entries.
- Decimal money representation and explicit currency.
- Posted records immutable; corrections by reversal/adjustment.
- Segregation of duties for create/approve where appropriate.
- Approval thresholds configurable by school/group.
- Reconciliation source and reviewer preserved.
- Idempotency for payment and posting requests.
- Accounting period lock and controlled reopen.
- Daily cash closing and variance workflow.

## 5. Offline security

- Register devices and their authorized user/scope.
- Cache minimal student data.
- Expire local datasets and show last refresh.
- Provide remote device revocation and session invalidation.
- Clear local data on sign-out where feasible and policy requires.
- Do not make offline approvals equivalent to server-authorized approvals.
- Make local pending data and rejected sync operations visible.
- Evaluate risks of shared computers and browser profile access.

Browser local storage is not a secure vault. Minimize sensitive cache and review encryption/key management realistically.

## 6. Proposed non-functional targets (to validate)

| Attribute | Initial target/hypothesis |
|---|---|
| Availability | 99.5% monthly initial production target |
| Normal API latency | p95 < 500 ms for typical requests, excluding long reports/AI |
| Financial integrity | No duplicate ledger posting; balanced journals |
| Offline recovery | Every accepted operation eventually acknowledged or explicitly flagged |
| RPO | Initial target 15 minutes, subject to database/backup configuration |
| RTO | Initial target 4 hours |
| Tenant security | Zero unauthorized cross-tenant access in release tests |
| Audit | Complete traceability for sensitive financial and academic actions |
| Accessibility | Keyboard-accessible forms and readable responsive layouts |
| Browser support | Current supported major browser versions, tested on actual target devices |

These are proposed targets, not measured performance or contractual SLAs.

## 7. Reliability and operations

- Structured logs with correlation IDs.
- Metrics for API, DB, sync, jobs and external providers.
- Error monitoring and alerting.
- Health/readiness endpoints.
- Database migration and rollback plan.
- Backup encryption and off-host backup copies.
- Scheduled restore drills.
- Deployment rollback procedure.
- Incident response runbook.
- Capacity monitoring for storage and AI costs.

## 8. Required security test cases

- User from Tenant A attempts to fetch Tenant B student by ID.
- Campus cashier attempts to read another campus ledger.
- Parent changes student identifier in API request.
- Revoked user/device submits queued offline operations.
- A cached connection reuses database pool after tenant context changes.
- AI Copilot asks for records outside user scope.
- Malicious prompt/document attempts to instruct AI to expose hidden data.
- Unauthorized user attempts discount, waiver, refund, marks approval or result publication.
- Bulk export attempts to bypass row-level permissions.


## 9. Hybrid Local Node security overlay (v1.1)

The hybrid deployment introduces local servers and a cloud-to-node trust relationship. Apply the following additional requirements:

- Local Node must operate on a hardened supported Linux host, with restricted management access and local HTTPS.
- PostgreSQL must not be publicly reachable and must use a least-privilege application role.
- Local authentication and authorization must remain available during internet outages.
- Every Local Node has a unique cryptographic identity; outbound sync uses mTLS or equivalent node authentication.
- Cloud license leases are node-bound and signed asymmetrically. Private signing keys are never distributed to Local Nodes.
- Local Nodes cannot be remotely revoked while completely disconnected; lease expiry and this limitation must be part of the contract and user-visible service status.
- Signed updates must be verified before installation; pre-upgrade backups and database migration safeguards are mandatory.
- Local backups and encrypted off-site backups are required; restore drills must be measured.
- Parent portal data, if replicated to cloud, must be minimum-necessary and show freshness.
- A local customer administrator may be able to tamper with the host if granted root access; managed-appliance terms and threat model must address this.

See `16_LOCAL_SECURITY_SYNC_AND_OPERATIONS.md` for the operational baseline.


## 10. Secure contribution and AI-assisted development baseline

Root [`AGENTS.md`](../AGENTS.md) is the authoritative repository-wide contributor and AI-agent guide. It defines mandatory controls for server-derived scope, tenant isolation, secrets/PII, offline command validation, AI safety, test evidence and pull-request review. DOC-61 records why these practices were adopted and how they fit the two-plane deployment.

Minimum execution rules:

- Never weaken authorization, tenant filters, validation, antiforgery, audit or security tests to make a feature/build pass.
- Never put credentials, signing keys, tokens, connection strings with secrets, or identifiable student/guardian/staff data in source, tests, logs, documentation, screenshots or prompts.
- Derive tenant/campus/resource scope server-side and check both user permission and resource relationship.
- Review every external provider, telemetry destination, CDN, payment integration and AI route for the data it receives, retention and security before enabling it.
- Run and report actual build/test results; do not infer production readiness from scaffolds, placeholder modules or a documentation checklist.
- Use pull requests and human review. AI agents must not approve or merge their own changes or bypass repository controls.
- Use secret/dependency scanning, analyzers, build/test and selected security checks as CI controls only after their workflows are implemented and successful runs are available as evidence.

Security-framework mappings are a control-discovery aid only. They do not establish that SchoolOS is certified or compliant. See DOC-61 and DOC-54.


## 11. SaaS tenant implementation security contract

Use [DOC-62](62_SAAS_TENANT_IMPLEMENTATION_BLUEPRINT.md) as the implementation companion for SaaS tenant identity, server-derived TenantContext, Cloud RLS/connection-pool controls, single-tenant Local Node binding, tenant provisioning and node enrollment. The baseline security requirements in this document still apply. Before production, close the relevant DOC-60 decisions and verify cross-tenant, missing-context, RLS/pooling, offline identity, sync ownership and tenant restore tests. DOC-62 is a design proposal; it is not evidence of implemented controls or legal compliance.
