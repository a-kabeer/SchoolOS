# 48 — Hybrid Deployment, Operations and Recovery Runbook

- **Document ID:** DOC-48
- **Status:** Draft
- **Decision maturity:** Proposed operating procedures; environment-specific approval required
- **Owner:** Operations lead (accountable role; assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
**Version:** v1.8 draft · **Status:** Operating procedure baseline; environment-specific values require approval  
**Applies to:** Cloud Control Plane and Local Data Plane  
**Companion:** docs/14_HYBRID_CLOUD_LOCAL_ARCHITECTURE.md, docs/15_SUBSCRIPTION_LICENSING_AND_OFFLINE_ENFORCEMENT.md, docs/16_LOCAL_SECURITY_SYNC_AND_OPERATIONS.md, docs/17_HYBRID_DEPLOYMENT_TASK_PLAN.md

## 1. Purpose and operating principles

This runbook defines the operational procedures required for SchoolOS deployments. It is not a record of completed infrastructure.

- The Local Node is authoritative for local school operations.
- Cloud Control Plane is authoritative for subscription, platform registry, signed license issuance and cloud-side service metadata.
- Local operations must not require a live Cloud Control Plane check for every request.
- Local PostgreSQL is not exposed to the public internet.
- Local Node starts outbound authenticated sync; no permanent inbound cloud management port is opened by default.
- School-owned hardware is the current commercial preference, but hardware/OS/support responsibilities must be agreed in each order form.
- PWA caching is supplementary; the Local Node remains the offline backend.
- One shared group node is supported only when campuses can reach it over a private network. Independent branch operation requires a node at each branch that must work disconnected.

## 2. Deployment profiles and topology selection

Before installation, complete a signed Deployment Profile Record.

| Profile | Description | Connectivity expectation |
|---|---|---|
| Cloud Centralized | Campus users connect to a cloud-hosted operational application | Reliable internet required for operations unless separately augmented |
| Hybrid shared-group node | One Local Node serves multiple campuses | Branches must retain private network/VPN access to the node |
| Hybrid per-campus edge nodes | Each autonomous campus has a Local Node and operational database | Each node can work through internet/WAN outages |
| Disconnected local + manual exchange | Each campus has a Local Node; controlled packages consolidate reporting at head office | No campus-to-campus internet dependency; reports are snapshots |

Selection questions:
1. Which campuses must keep working if the internet is down?
2. Which campuses must keep working if the WAN/head-office connection is down?
3. Is there a private LAN/VPN path to a shared node?
4. Who owns the server, UPS, LAN, firewall and OS patching?
5. Which records may be synchronized or exported to cloud/head office?
6. Is remote parent access required, and what minimum data projection is allowed?
7. What support response and recovery service can actually be provided?

Do not promise independent offline operation for a campus that has no Local Node and cannot reach a shared node.

## 3. Environment separation

Maintain separate:
- Developer environment.
- Automated test/CI environment.
- Staging environment.
- Production Cloud Control Plane.
- Production Local Data Plane for each customer/site.
- Dedicated synthetic Demo Runtime store/environment, isolated from customer operational data.

Use synthetic data in CI and demo. Production student, parent, financial and academic data must not be copied into demo or developer environments.

Secrets:
- Never commit production passwords, signing keys, node private keys, provider API keys, database credentials or support credentials.
- Use a secret manager or approved protected host secret store.
- Separate development/staging/production keys.
- Rotate credentials on compromise and planned security schedule.
- Ensure frontend bundles contain no secret credentials.

## 4. Local Node provisioning checklist

### 4.1 Before installation

- [ ] Approved customer, tenant, school and campus identity.
- [ ] Approved node topology and node/site ownership.
- [ ] Supported Linux distribution/version agreed.
- [ ] CPU/RAM/storage sizing agreed from workload and retention assumptions.
- [ ] UPS and controlled shutdown process available.
- [ ] LAN DNS/hostname and local HTTPS certificate plan ready.
- [ ] Firewall policy reviewed; PostgreSQL private only.
- [ ] Backup target and encryption key custody agreed.
- [ ] OS administrator and SchoolOS application operator responsibilities named.
- [ ] Local identity/admin bootstrap contact verified.
- [ ] Licence lease and node registration process ready.
- [ ] Data import/migration templates and sample validation completed.

Do not hard-code minimum hardware in this document until a measured pilot profile is approved.

### 4.2 Install sequence

1. Install and patch the supported OS.
2. Create a dedicated non-root service identity for SchoolOS.
3. Configure host firewall, SSH/admin access and time synchronization.
4. Configure persistent directories/volumes for PostgreSQL, files, logs, backups, license snapshots and sync queues.
5. Install the signed SchoolOS release artifact and verify its signature/digest.
6. Configure local HTTPS and trusted LAN hostname.
7. Start PostgreSQL with a restricted application role; bind database port to private/local interfaces only.
8. Apply the approved database migration bundle through the controlled migration runner.
9. Bootstrap the Local Node identity and verify the node certificate/credential.
10. Obtain/import the signed node-bound licence lease.
11. Initialize the authorized local administrator; require password reset/MFA according to policy.
12. Confirm local login and core health/readiness endpoints.
13. Import approved reference data/configuration.
14. Run tenant/campus isolation smoke tests.
15. Run local backup and restore verification.
16. Run outbound sync connectivity test.
17. Sign the installation handover record.

The current repository's Docker Compose is a local development PostgreSQL service only; it is not a complete production Local Node installer.

## 5. Cloud Control Plane provisioning

- [ ] Provision separate Cloud Control Plane database and protected secrets.
- [ ] Deploy Cloud API and required background workers.
- [ ] Configure admin/customer/portal/demo authentication boundaries.
- [ ] Configure MFA for privileged staff.
- [ ] Configure signing service/KMS/HSM custody where feasible.
- [ ] Create tenant/customer registry record.
- [ ] Create node registry entry and node identity enrollment.
- [ ] Configure subscription/entitlements and manual payment verification policy.
- [ ] Publish signed release manifest.
- [ ] Configure Sync Hub queues/cursors and node monitoring.
- [ ] Configure backup catalogue and retention policy if enabled.
- [ ] Verify public, portal, admin and demo trust boundaries.
- [ ] Run security, webhook and tenant-isolation smoke tests.

A cloud tenant record does not imply that customer operational student data is stored in the cloud.

## 6. Health and monitoring

### 6.1 Local Node health

Monitor:
- API process liveness and readiness.
- PostgreSQL availability, connections, locks, disk and WAL growth.
- File/object storage capacity and permissions.
- Pending sync queue size and oldest event age.
- Failed sync attempts and conflict count.
- Last successful backup and restore test date.
- Licence state, paid-through date, lease expiry and clock rollback warnings.
- Disk/CPU/memory/temperature where available.
- Certificate expiry.
- Application error rate and response latency.
- Migration version and release version.

### 6.2 Cloud Control Plane health

Monitor:
- Cloud API and database availability.
- Subscription/payment verification queue.
- Licence signing and issuance health.
- Node last-seen/heartbeat and sync freshness.
- Event backlog, retry and dead-letter/conflict queues.
- Release signature and rollout state.
- Admin audit and security alerts.
- BCP demo cleanup backlog and failed cleanup alerts.
- Provider webhook verification failures and idempotency conflicts.

### 6.3 Operational status labels

Use explicit, user-facing status:
- Online / Local.
- Cloud disconnected.
- Pending Sync.
- Synced / Acknowledged.
- Conflict / Needs Review.
- Last Backup at [timestamp].
- Licence Valid / Grace / Restricted.
- Data Snapshot: Last imported [timestamp].
- Parent portal data may be stale.

Do not show a stale projection as Live.

## 7. Backup procedure

### 7.1 Backup scope

A complete recoverable backup may need:
- PostgreSQL database.
- Private documents and attachment storage.
- Required configuration (excluding secrets where possible).
- Data Protection/key material required to decrypt protected state, stored under separate custody.
- Local sync queues/outbox and pending operations.
- Licence snapshot and node activation metadata.
- Migration version and release manifest.
- Backup metadata/checksum.

A database-only backup is not a complete SchoolOS recovery package if document storage or keys are required to restore records.

### 7.2 Backup controls

- Use encrypted backup at rest and in transit.
- Keep at least one off-host copy when approved by customer policy.
- Use unique backup identifiers, timestamps, DB version, schema/migration version, node ID and checksum.
- Protect encryption keys separately from backup files.
- Retain backups according to an approved schedule and customer/legal policy.
- Do not include ephemeral demo workspaces in long-lived production/customer backups.
- Alert on backup failure, unexpected size reduction and stale backup age.
- Document backup when internet is unavailable and later off-site transfer.

### 7.3 Backup restore drill

At the approved frequency:
1. Select a recent encrypted backup.
2. Provision a clean replacement/test host.
3. Restore database and private files.
4. Restore required protected configuration/key access.
5. Apply compatible migration/release steps where approved.
6. Verify record counts/checksums and sample workflows.
7. Verify node/license reactivation follows node-replacement policy.
8. Verify pending sync/outbox state and duplicate protection.
9. Record start/end time, recovery point, errors and measured RPO/RTO.
10. Obtain operations sign-off.

RPO 15 minutes and RTO 4 hours are current planning targets only; customer-specific contracts require demonstrated measurements.

## 8. Upgrade and rollback procedure

1. Confirm release signature, digest, target node type and compatibility manifest.
2. Confirm free disk and healthy DB/storage.
3. Confirm a recent restorable backup.
4. Enter approved maintenance window and notify customer.
5. Drain/stop new application work as specified for the release.
6. Keep sync queues durable; do not discard pending operations.
7. Apply schema migration using one controlled runner and migration lock.
8. Deploy the signed application artifact.
9. Run readiness checks and smoke tests.
10. Resume service and sync worker.
11. Watch error rate, queue backlog, DB load and user-reported issues.
12. Record release, migration IDs and outcome.

If a migration is destructive or not safely reversible, do not assume binary rollback is enough. Use a documented forward-fix or restore plan. Never downgrade schema blindly after new-format writes have occurred.

## 9. Cloud outage and sync recovery

When the Local Node cannot reach cloud:
- Confirm LAN and Local API are healthy.
- Confirm the current signed licence remains valid or inside approved grace.
- Continue only operations listed as locally available.
- Keep Outbox/Inbox queues durable.
- Show cloud-dependent parent/remote/reporting features as unavailable or stale.
- Do not relabel pending cloud work as completed.

On reconnect:
1. Re-establish node authentication (mTLS or approved equivalent).
2. Validate node status, tenant binding and schema compatibility.
3. Resume from last acknowledged cursor.
4. Deliver with bounded retry/backoff.
5. Apply Inbox idempotency and aggregate-version checks.
6. Route conflicts to review; do not use universal last-write-wins.
7. Show per-node queue and freshness until acknowledged.
8. Record sync completion and failures.

## 10. Manual campus data exchange recovery

For disconnected manual exchange:
- Generate a signed, recipient-bound encrypted package from the source node.
- Validate signature, tenant, school, campus, node, schema/module version, checksum and export watermark.
- Run dry-run before apply.
- Check already-imported package/event IDs.
- Review creates/updates/skips/rejections/conflicts.
- Obtain approval from the receiving operator.
- Apply in bounded batches with durable import log.
- Verify counts and finance totals.
- Record acknowledgement and package provenance.
- Keep imported branch-owned data read-only by default.

Never import an arbitrary PostgreSQL dump from removable media as the normal interchange path.

## 11. Licence expiry and recovery

- Cloud subscription state is authoritative in the Control Plane.
- Local Node validates the latest signed node-bound lease.
- Automatic online renewal and manual signed .lic import use the same validation and atomic activation pipeline.
- A disconnected Local Node cannot receive immediate remote revocation; communicate this product limitation.
- Grace duration remains open; seven days is a proposal, not a final contractual promise.
- After approved grace, Restricted Mode preserves historical read/export, backup, billing/licence renewal and support while blocking specified operational writes.
- Renewal reactivates the existing installation without requiring a software update.
- Validate signature, issuer, audience, tenant, node, sequence, validity dates and entitlements.
- Never store the cloud signing private key on a Local Node.

## 12. Node replacement / failure

1. Open an incident and identify node/customer/campus scope.
2. Confirm backup age and availability.
3. Provision replacement hardware/host using the approved build.
4. Restore database, files and required configuration.
5. Do not reuse the old node identity blindly.
6. Use the approved node replacement/transfer workflow and audit it.
7. Issue a replacement node-bound licence lease.
8. Revalidate permissions and device registrations.
9. Confirm backup and sync queue state.
10. Reconnect to Sync Hub using controlled cursor/replay behavior.
11. Verify no duplicate financial or academic operations.
12. Obtain school/technical-lead acceptance before closing incident.

Exact lost-node revocation and replacement rules remain an Open Decision until approved.

## 13. Security incident response

For suspected credential leak, data exposure, malware or unauthorized access:
1. Record incident time, reporter, affected node/tenant and symptoms.
2. Preserve logs/evidence; do not destroy affected state.
3. Contain affected account/device/token/node using available revocation controls.
4. Escalate to named security/operations owner.
5. Assess whether Local Node is offline and what revocation delay applies.
6. Rotate compromised credentials/keys under a reviewed rotation plan.
7. Determine data accessed/exported and affected customer scope.
8. Notify customer/regulator/individuals according to approved legal policy.
9. Restore service from trusted state if needed.
10. Record root cause, corrective actions and lessons learned.

This runbook is operational guidance, not legal advice. Notification duties and deadlines require qualified Pakistan-specific legal review.

## 14. Support access operations

- Support ticket and customer approval are required for access beyond normal diagnostics.
- Grant includes named agent, purpose, time window, node/campus scope and permission level.
- Default is read-only, field-minimized diagnostics.
- Elevated access requires separate approval and step-up authentication.
- Record all access and changes in immutable audit.
- Automatically expire grants and invalidate sessions.
- Do not expose a generic remote shell or database login to support staff by default.

## 15. Customer handover record

Each deployment must retain:
- Customer/tenant/school/campus and Local Node IDs.
- Deployment profile and network topology.
- Hardware/OS/app/database versions.
- Local administrator and support contacts (securely stored).
- Licence status and renewal method.
- Backup frequency/location/encryption owner.
- Restore test date and measured RPO/RTO.
- WAN/cloud outage behavior and offline scope.
- Data sync/export domains and freshness expectations.
- Support hours/escalation path.
- Customer acceptance and handover date.

## 16. Open operating decisions

- Supported Linux distribution and patch cadence.
- Hardware profiles for 200/500/1,000+ student deployments.
- UPS and customer network requirements.
- Backup frequency, retention, off-site target and key custody.
- RPO/RTO per plan and measured service tier.
- License lease duration, grace and trusted-time/clock rollback policy.
- Node replacement and deactivation rules.
- Support hours, on-site support pricing and incident escalation.
- Parent portal projection and data freshness target.
- Manual media encryption/transport/destruction procedure.
- Data export/retention at subscription termination.

No environment is production-ready until these decisions are converted into customer-specific configuration and tested procedures.
