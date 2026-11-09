# 38 — Business & Customer Platform Security Requirements

**Version:** v1.7 · **Status:** Security baseline proposal; formal threat model Not Started

## 1. Trust boundaries

1. Public internet visitor → public website/lead APIs.
2. Demo user → isolated synthetic demo environment.
3. Customer portal user → organization-scoped commercial metadata.
4. Internal staff → privileged Cloud Control Plane operations.
5. Cloud Control Plane ↔ Local Data Plane nodes via authenticated outbound synchronization/lease channels.
6. Third-party providers → webhook and notification integrations.

Treat all inputs from browser, demo users, external AI models, customer attachments, WhatsApp and webhooks as untrusted.

## 2. Identity and session boundaries

- Separate authentication audiences and session/cookie boundaries for portal, internal admin and demo; public/demo sessions cannot be upgraded to customer or admin. Demo has its own cookie namespace and server-side revocation state.
- Require MFA for staff, especially Super Admin, Finance, Licence and Support access roles.
- Use short-lived access sessions, refresh/session rotation, revocation, secure HttpOnly cookies, SameSite and CSRF defenses where cookies are used.
- Apply password hashing using a current, recommended password-hashing scheme; never store reversible passwords.
- Provide login throttling, credential stuffing protection, account recovery controls and security notifications.
- Enforce step-up authentication for high-impact actions such as payment verification, licence issuance/revocation, staff role changes and support access grants.

## 3. Authorization and tenant isolation

- Default deny; protected endpoints require authentication and explicit authorization.
- Derive organization membership and campus scope from verified identity claims/membership records, not request body/query values.
- Enforce object-level authorization on every read and mutation, including downloads, attachments, exports, ticket IDs and licence files.
- Apply least privilege and separation of duties across Sales, Finance, Licence, Support, Operations and CMS roles.
- PostgreSQL RLS may be added as defense in depth; it does not replace application authorization.
- Test horizontal and vertical privilege escalation, IDOR, organization switching and stale role/session behavior.

## 4. Admin and support access

- Admin app uses a separate privileged sign-in surface and MFA.
- Staff cannot automatically browse student/guardian/fee/exam records.
- Customer-approved support access has a stated purpose, node/scope, named agent, start/end, default read-only permission, approval and audit trail.
- Access expires automatically; emergency break-glass workflow is separately approved, time-limited and reviewed.
- No publicly exposed PostgreSQL ports or persistent generic remote shell by default.
- Impersonation is disabled by default; if ever introduced, it requires strong controls, visible banner, reason, customer notification policy and immutable audit.

## 5. Data and cryptographic controls

- HTTPS/TLS for all network paths; consider mTLS for node-to-cloud identity and sync.
- Encrypt sensitive data at rest and protect backups; keys managed separately from encrypted data.
- Licence private signing key remains in isolated KMS/HSM or controlled signing service; public verification key is distributed to Local Nodes.
- Never put secrets in source control, frontend bundles, logs or database rows without an approved encrypted key-management design.
- Minimize student/customer information sent to cloud; keep campus-local operational data local by default.
- Apply data classification, retention, deletion, export and incident processes.

## 6. Application and integration security

- Validate inputs and encode outputs; apply CSP, XSS protections, security headers and CORS allowlists.
- Rate-limit login, registration, demo, contact, uploads, chat and webhook endpoints.
- Verify webhook signatures/timestamps and implement replay protection/idempotency.
- Malware-scan and restrict attachment content types, sizes and archive expansion.
- Store provider API keys in secret manager; restrict scopes and rotate credentials.
- Use safe redirects, SSRF protections and outbound allowlists for URL-fetching integrations.
- Record security-relevant events without logging passwords, tokens, signing secrets, raw sensitive prompts or full student data.

## 7. Demo and marketing protections

- Synthetic data only; no customer database copies.
- Temporary session-bound workspace, separate Demo Runtime store, immutable seed template, resource quotas, reset and anti-abuse controls.
- Logout revokes access immediately. Last-tab close is best-effort and backed by server-side client leases, reconnect grace, inactivity/absolute expiry and periodic cleanup. Refresh/multiple tabs must not accidentally destroy a live workspace.
- Idempotent cleanup covers database, temporary files/objects, caches, queues, search/vector artifacts and temporary runtimes. Failed cleanup is retried and alerted.
- Exclude disposable demo data from long-lived production/customer backups; do not log demo record payloads. Document the residual lifetime of immutable infrastructure snapshots.
- Demo external side effects disabled (real payments, bulk notifications, licence signing).
- Consent/preferences respected for marketing analytics and communications.

## 8. Secure delivery and operations

- Threat model and abuse cases before launch.
- Dependency scanning, secret scanning, static analysis, container/image scanning and code review.
- Automated tenant isolation, authorization matrix, webhook replay, rate-limit and upload-safety tests.
- Centralized security logs, alerting, backup/restore drills and incident response playbook.
- Penetration test before production and after material auth/tenant architecture changes.
- Define RPO/RTO, vulnerability remediation targets, patch process and key rotation runbook.

## 9. Acceptance gate

No production launch until critical/high security findings are resolved or formally accepted, admin/portal tenant isolation tests pass, MFA and audit are operational, signing key custody is verified, backup restore is tested, support-access lifecycle is tested and incident response ownership is assigned.


## Ephemeral demo session security update (v1.7)

See `docs/41_EPHEMERAL_SESSION_BASED_DEMO_DESIGN.md` and ADR-021. Browser unload callbacks must never be the sole expiry or deletion mechanism. Validate server-side status on every request, revoke immediately on explicit logout, prevent cross-session access through server-derived scope and RLS, and treat physical cleanup as an observable asynchronous process.
