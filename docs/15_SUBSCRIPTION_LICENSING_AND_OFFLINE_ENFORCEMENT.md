# 15 — Subscription, Licensing and Offline Enforcement

**Version:** 1.2  
**Status:** Recommended commercial and technical policy; final prices, grace period and retention terms require owner approval.

## 1. Core decision

SchoolOS will sell recurring subscriptions while running customer operations on local SchoolOS nodes. Subscription enforcement must therefore work without a permanent connection to the Cloud Control Plane.

Use a **signed, time-bounded offline license lease**. The cloud issues it; the local node validates it using a public verification key. Local nodes must not contain the cloud private signing key or a shared secret that can mint licenses.

**Confirmed renewal policy (9 November 2026):** support BOTH (1) automatic lease renewal when a Local Node has internet connectivity and (2) manual signed `.lic` file download/import as an offline fallback, including an approved USB transfer path. The manual file is a licence renewal, not a software update. A valid renewal should reactivate the existing installation without requiring a new application build or database migration.

**Confirmed expiry policy (9 November 2026):** after the grace period, use Restricted Mode—not a destructive shutdown or data lockout. Preserve data and allow authorized historical viewing, reports/export, backup/restore administration, billing/licence renewal and support diagnostics. Block new operational writes, including admissions, attendance, fee collection/receipt creation, marks and new financial postings. The exact operation-by-operation matrix must be finalized and tested before release.

## 2. Separate billing status from local license status

Billing status is maintained by the cloud. The local node has a signed snapshot of the latest allowed entitlement. These are related but not identical.

### Subscription lifecycle (cloud source of truth)

- `Trial`
- `Active`
- `PastDue`
- `GracePeriod`
- `Suspended`
- `Canceled`
- `Expired`

### Local license lifecycle

- `Valid`
- `ExpiringSoon`
- `Grace`
- `Restricted`
- `InvalidSignature`
- `Expired`
- `RevokedOnNextContact`
- `ClockRollbackReview`

A local node cannot know about a cloud cancellation/revocation while completely disconnected. This is a fundamental limitation, not a coding defect. While connected, the node should check at startup and at a scheduled heartbeat interval (for example, every 12 hours); the exact interval is configurable and must not block ordinary local operations.

## 3. Subscription product model

Recommended commercial dimensions:

- Tenant / school group.
- Licensed school entities and campuses.
- Maximum active student capacity (define whether active enrollment or unique active students is counted).
- Number of local nodes/sites.
- Enabled modules/features.
- AI usage quota or credits.
- Storage/backup quota.
- Support tier and update entitlement.

Avoid hard-coding prices in the application. Store plans, prices, currencies, tax configuration and billing periods in the Cloud Control Plane.

A hybrid customer may also pay a one-time installation/appliance fee or recurring managed-hardware/support fee. Hardware ownership, replacement and support must be explicit in the order form.

## 4. Signed offline license lease

### 4.1 Issuance

1. Cloud billing confirms the subscription payment or authorized manual payment verification.
2. Entitlement service calculates the paid-through date, grace boundary, limits and allowed features.
3. Cloud License Service creates a signed license document.
4. Local node obtains it either through automatic authenticated outbound renewal or through an approved manual `.lic` file import (downloaded from the customer portal and transferred by USB/other approved method).
5. Local License Verifier checks signature, node identity, tenant scope, dates, sequence and entitlement limits.
6. Local license snapshot is stored durably and audited.

### 4.2 Recommended claims

The signed document should contain:

- License ID and schema/version.
- Tenant ID and customer identifier.
- Node ID and licensed site/campus scope.
- Plan ID and entitlement identifiers.
- Limits: student capacity, campuses, nodes and relevant quotas.
- `issued_at_utc` and `not_before_utc`.
- `paid_through_utc`.
- `grace_until_utc`.
- `lease_expires_at_utc`.
- Monotonic license sequence number.
- Signing key ID (`kid`).
- Optional hardware/TPM public-key thumbprint.

Do not place passwords, private keys or unnecessary student data in a license document.

### 4.3 Cryptographic design

- Use a standard, reviewed signed-token format such as JWS with ES256 (ECDSA P-256 + SHA-256), or another approved standard supported by maintained .NET libraries. Do not invent a signature format.
- Keep the signing private key in a cloud-managed secret store/HSM where feasible.
- Ship only trusted public verification keys to Local Nodes.
- Include a key ID (`kid`) and support key rotation with an overlap period.
- Validate algorithm allow-lists, issuer, audience, tenant, node, time claims, sequence and entitlement claims. Reject `none`, unexpected algorithms and unknown keys.
- Do not use an unsigned JSON file, a client-editable database flag or a shared HMAC secret embedded in every Local Node as the license authority.

A code sample must not be written until the library, claims, key storage, key rotation and cryptographic test vectors are reviewed.

## 5. Proposed monthly subscription lifecycle

Initial policy proposal (commercial owner can change it):

| Time | Node behaviour |
|---|---|
| Before paid-through date | Full licensed service; show renewal reminders |
| Paid-through date | Show payment due warning; continue during grace |
| Grace period (proposed 7 calendar days) | Full normal service; prominent overdue warnings |
| After grace boundary | Restricted mode: no new operational writes; permit billing, local admin, backup, data export and license renewal |
| Reactivation | Verify new signed lease; restore entitlements; process queued events under explicit validation/review rules |

Do not delete data or destroy the database because a subscription expired. Contractual data-export and retention windows must be defined separately.

### Offline expiry rule

The local node can enforce only the signed lease it has received. The recommended `lease_expires_at_utc` is no later than the current subscription's `grace_until_utc`. If the subscription is renewed in the cloud while the node is disconnected, the node cannot know until it receives a new lease.

For a paid customer whose Local Node is still disconnected, support may issue an offline renewal file through the cloud billing portal. The customer imports it on the local admin screen or through an approved USB procedure. The file remains cryptographically signed and node-bound.

## 6. Offline renewal and payment verification

Manual fallback process (automatic renewal is preferred whenever the node is online):

1. Customer pays by an approved bank/payment channel.
2. Cloud billing administrator or payment gateway verifies receipt.
3. Cloud issues a new node-bound signed lease.
4. Customer downloads the renewal file on an internet-connected device.
5. Authorized Local Administrator imports the file into the Local Node.
6. Local node validates signature, tenant, node, sequence and dates; then records a license audit event and restores the valid entitlements without requiring a software update.

A payment screenshot alone must not activate a subscription. Manual payment verification requires an authorized billing operator and a traceable payment reference.

## 7. Time rollback and tampering limitations

An offline server cannot obtain trusted current time from the cloud while disconnected. A customer with operating-system administrator/root access may alter system time or patch local software.

Mitigations:

- Persist the last trusted server time and a monotonic license sequence.
- Detect system time moving backward beyond an allowed skew and enter review/restricted mode.
- Record monotonic elapsed time where the host provides a reliable mechanism.
- Use TPM-backed node identity and secure boot where supported by managed hardware.
- Restrict customer access to operating-system root on a SchoolOS-managed appliance.
- Sign software releases and verify signatures before updates.
- Keep license enforcement in the local API, not only in React UI.
- Add contractual subscription, use, audit and termination terms.

**Honest limitation:** If the customer controls the hardware, OS administrator/root account and executable files, no purely software-based license mechanism can guarantee perfect prevention of tampering. A disconnected node also cannot be remotely revoked immediately. Managed hardware and contractual controls reduce the risk; they do not make it mathematically impossible.

## 8. Managed appliance recommendation

Offer two operational models:

### SchoolOS-managed appliance (preferred for strict subscription enforcement)

- SchoolOS supplies or manages the Local Node hardware.
- School receives school-level application administration but not unrestricted OS root access.
- SchoolOS handles signed updates, monitoring and recovery through approved remote support.
- Remote support requires customer approval, MFA, a time limit and a complete session audit.

### Customer-managed server

- Customer controls the host and OS administrator account.
- License lease still works, but enforcement is weaker because the customer can modify the host.
- Use written licensing terms and customer security responsibilities.

The sales agreement must state who owns hardware, who handles electricity/network/UPS, who patches the OS, who backs up data and who can access the machine.

## 9. Restricted mode policy

**Confirmed product policy:** when the signed license reaches its grace boundary, the Local Node enters Restricted Mode. Do not delete, encrypt, hide or make customer data inaccessible as a subscription enforcement mechanism. The exact grace duration remains a configurable commercial policy; seven calendar days is still a proposal, not an approved final term.

Allowed:

- Local login for authorized owner/admin/billing contact.
- View existing student, academic, attendance, fee and financial records in read-only form.
- View and generate historical reports from existing data.
- Subscription status and renewal instructions.
- Data export in supported formats.
- Backup and restore administration.
- License import/renewal.
- View existing records in read-only form.
- Upload/sync already pending operations into a quarantine/review state to avoid data loss, subject to security and license terms.
- Support and security diagnostics.

Blocked:

- New admissions and student lifecycle changes that create or materially alter operational records.
- New attendance entries/corrections and marks/assessment writes.
- New fee collection/receipt creation and new fee adjustments.
- New operational financial postings, including new expenses, refunds and ledger postings unless an explicitly approved recovery exception is designed.
- New AI jobs that consume paid provider credits.
- New document uploads beyond recovery needs.

Existing data remains intact. Once renewed, either the automatic renewal agent (online) or the manual licence importer (offline) installs the new signed lease. After validating the new lease, the node restores the entitlements on the existing installation; no software update is required. Any queued/pending operations must be validated and reviewed before posting. Queued data must undergo idempotency and business validation; it must not be blindly posted.

## 10. Cloud-side database additions

Suggested tables (exact columns and constraints to be designed):

- `platform.subscription_plans`
- `platform.plan_entitlements`
- `platform.subscriptions`
- `platform.subscription_items`
- `platform.subscription_invoices`
- `platform.subscription_payments`
- `platform.subscription_events`
- `platform.license_nodes`
- `platform.license_leases`
- `platform.license_signing_keys`
- `platform.node_activations`
- `platform.node_revocations`
- `platform.release_manifests`
- `platform.update_rollouts`
- `platform.billing_contacts`

## 11. Local-side database additions

Suggested tables:

- `platform_local.node_identity`
- `platform_local.license_snapshots`
- `platform_local.license_validation_events`
- `platform_local.entitlement_cache`
- `platform_local.device_registrations`
- `platform_local.local_admin_actions`
- `sync.outbox_events`
- `sync.inbox_events`
- `sync.cursors`
- `sync.conflicts`
- `sync.delivery_attempts`

The local database must retain license evidence and history without storing the cloud signing private key.

## 12. Automatic and manual renewal behavior

### Automatic renewal (connected Local Node)

- Local renewal agent authenticates using the node's registered identity (prefer mTLS or equivalent device-bound credentials).
- It checks for a new lease at startup and on a configurable schedule; a suggested initial heartbeat is every 12 hours, subject to operational testing.
- Cloud verifies current subscription/payment status and issues the next signed lease.
- Local node verifies and atomically stores the new lease, records an audit event and updates entitlements.
- If renewal fails because the network is unavailable, the node continues using the last valid signed lease until its allowed boundary; it must not block normal requests while waiting for cloud.
- If cloud reports unpaid/suspended/cancelled status, the node applies the policy at the signed lease boundary or on authenticated receipt of a signed status/revocation update, according to the documented commercial rules.

### Manual renewal (disconnected Local Node)

- Authorized customer downloads the node-specific `.lic` file from the customer portal after payment verification.
- An authorized Local Administrator imports it through the local License screen or approved USB procedure.
- The importer validates the complete signed claims, expected tenant/node identity, validity window and monotonic sequence before activation.
- The import is atomic: invalid files do not overwrite the currently stored valid lease.
- Show the resulting status, paid-through date, grace boundary and node identity to the administrator; log who imported it and when.

Automatic and manual renewal must share the same validator and activation pipeline. Do not implement two separate cryptographic validation paths.

## 13. API surface (illustrative)

Cloud Control Plane:

- `POST /api/v1/nodes/activate`
- `POST /api/v1/nodes/{nodeId}/lease/renew`
- `POST /api/v1/nodes/{nodeId}/lease/offline-renewal-request`
- `POST /api/v1/nodes/{nodeId}/revoke`
- `GET /api/v1/nodes/{nodeId}/releases`
- `POST /api/v1/sync/events/push`
- `GET /api/v1/sync/events/pull?cursor=...`

Local Node:

- `GET /api/v1/local/license/status`
- `POST /api/v1/local/license/import`
- `GET /api/v1/local/node/health`
- `POST /api/v1/local/sync/run`
- `GET /api/v1/local/sync/conflicts`

These endpoints are examples; final names, authentication, idempotency and API contracts must be specified before implementation.

## 14. Acceptance tests

- Tampered license content fails signature verification.
- License for Node A cannot activate Node B.
- Expired lease enters the configured restricted mode.
- A valid imported offline renewal restores valid status without requiring a software update.
- Automatic renewal succeeds when the node is online and the subscription is paid.
- Automatic renewal failure due to temporary network outage does not interrupt local operations before the lease boundary.
- Automatic and manual renewal use the same signature/claim validation and activation pipeline.
- Invalid manual import leaves the currently installed lease unchanged.
- Re-importing an older or duplicate sequence cannot roll back the active lease.
- Old/replayed license sequence cannot roll back a newer license.
- System clock rollback is detected and logged.
- Local node remains operational during a cloud outage until the signed lease boundary.
- Revocation is applied when a disconnected node reconnects; documentation clearly states it cannot be immediate while fully offline.
- Subscription expiry never deletes customer operational data.
- Restricted mode allows export, backup and renewal but blocks new operational writes.
- Pending offline events are preserved and reviewed after suspension/reactivation.
