# 17 — Hybrid Deployment and Subscription Work Package

**Version:** 1.1  
**Status:** Added to the master development plan.  
**Rule:** Hybrid deployment is a first-class product capability, not a late add-on.

## Work Package H0 — Deployment discovery

- [ ] Interview pilot schools about internet outages, LAN layouts, branch connectivity and power stability.
- [ ] Decide which customer requires one shared group node versus one node per campus.
- [ ] Define managed appliance versus customer-managed server offering.
- [ ] Define who owns hardware, OS patching, backups, connectivity and on-site support.
- [ ] Define maximum acceptable offline lease duration and grace period.
- [ ] Define student-data replication and cloud backup consent/retention.
- [ ] Define parent/student remote access requirements.
- [ ] Define AI availability during internet outage.
- [ ] Record decisions in ADRs and commercial order forms.

**Exit:** Each pilot customer has an approved deployment topology, responsibility matrix and outage expectation.

## Work Package H1 — Cloud Control Plane

- [ ] Create cloud platform host and separate control-plane database.
- [ ] Implement tenant/customer registry.
- [ ] Implement subscription plans and entitlements.
- [ ] Implement subscription invoices and payment records.
- [ ] Implement manual payment verification with audit history.
- [ ] Implement node registry and node activation workflow.
- [ ] Implement billing contacts and renewal reminders.
- [ ] Implement node health/last-seen dashboard.
- [ ] Implement release manifest catalogue.
- [ ] Implement administrator and support permissions.

**Exit:** A new tenant and Local Node can be registered, billed and observed without copying the local operational database to cloud.

## Work Package H2 — Local Data Plane packaging

- [ ] Package ASP.NET Core Modular Monolith for local deployment.
- [ ] Package React frontend and local HTTPS reverse proxy.
- [ ] Configure PostgreSQL local database and restricted roles.
- [ ] Add persistent storage for database, documents, logs, license snapshots, sync queues and Data Protection keys.
- [ ] Implement local identity/authentication for outage operation.
- [ ] Implement local health/readiness endpoints.
- [ ] Provide installation/bootstrap and recovery scripts.
- [ ] Create supported Linux image/host baseline.
- [ ] Create local DNS/TLS setup guide.
- [ ] Validate UPS and controlled shutdown behaviour.

**Exit:** A clean local host can be installed, initialized and used by school staff without live cloud connectivity after activation.

## Work Package H3 — License and entitlement enforcement

- [ ] Define canonical signed-license format and test vectors.
- [ ] Implement cloud signing key storage and key rotation.
- [ ] Implement node-bound signed lease issuance.
- [ ] Implement local public-key signature verification.
- [ ] Implement tenant/node/scope/feature/quota validation.
- [ ] Implement persistent license snapshot and sequence tracking.
- [ ] Implement trusted-time/clock-rollback detection.
- [ ] Implement automatic online signed-lease renewal for connected Local Nodes.
- [ ] Implement manual signed `.lic` file download/import and approved USB fallback for disconnected nodes.
- [ ] Route automatic and manual renewals through the same signature validator and atomic activation pipeline.
- [ ] Implement grace and confirmed Restricted Mode access matrix.
- [ ] Ensure valid renewal reactivates the existing installation without requiring a software update.
- [ ] Preserve customer data and allow export/backup after expiry.
- [ ] Implement support/admin audit for license actions.
- [ ] Test copied, tampered, replayed, wrong-node and expired license files.

**Exit:** Automatic online renewal and manual offline `.lic` renewal both work; expiry enters Restricted Mode without data loss; a valid renewal restores entitlements without a software update; technical limitations are documented and tested.

## Work Package H4 — Secure node-to-cloud synchronization

- [ ] Implement unique node identity and mTLS credentials.
- [ ] Implement local transactional Outbox and Inbox.
- [ ] Implement cloud Sync Hub inbox/outbox and per-node cursors.
- [ ] Implement event schema/version registry.
- [ ] Implement outbound sync agent and retry/backoff.
- [ ] Implement idempotent event application.
- [ ] Implement durable acknowledgements.
- [ ] Implement conflict records and review UI.
- [ ] Implement data ownership and one-writer rules per aggregate.
- [ ] Implement node revocation on next contact.
- [ ] Implement sync queue monitoring and diagnostics.
- [ ] Test multi-day outages, duplicated events, out-of-order events and node replacement.

**Exit:** Local operations are durable during cloud outages and synchronize without duplicate business effects.

## Work Package H5 — Backup, updates and local operations

- [ ] Implement scheduled local database/document backups.
- [ ] Implement encrypted off-site backup transfer (subject to customer policy).
- [ ] Implement backup catalogue and restore workflow.
- [ ] Implement signed update manifests and image digest verification.
- [ ] Implement staged update rollout and maintenance windows.
- [ ] Implement database migration lock and pre-upgrade backup gate.
- [ ] Implement diagnostics and health metrics.
- [ ] Implement support access approval, MFA and session audit.
- [ ] Complete server-hardening baseline.

**Exit:** Recovery, update and support operations have been tested on replacement hardware and with a disconnected node.

## Work Package H6 — Multi-campus topology

- [ ] Implement site/node registry for multiple campuses.
- [ ] Define node ownership for campus-specific and group-level records.
- [ ] Implement group-level user/role replication.
- [ ] Implement authorized student/guardian master-data routing.
- [ ] Implement campus-to-cloud-to-campus event routing.
- [ ] Implement consolidated reporting projections with stale-data timestamps.
- [ ] Implement cross-campus transfer workflow.
- [ ] Implement node deactivation and data migration workflow.
- [ ] Test branch isolation and shared-node topology.

**Exit:** A multi-campus customer can operate on its selected topology and clearly sees which group data is current versus awaiting synchronization.

## Work Package H7 — Pilot and hardening

- [ ] Pilot one shared-node school group.
- [ ] Pilot per-campus nodes where independent campus autonomy is required.
- [ ] Run internet outage, power interruption and local server restart drills.
- [ ] Run subscription expiry/grace/reactivation tests.
- [ ] Run local admin/root-tampering threat review.
- [ ] Run license key rotation/revocation tests.
- [ ] Run full data export and restore drill.
- [ ] Run tenant, site and role authorization tests.
- [ ] Review support responsibility and SLA language.
- [ ] Record issues, fixes and release approval.

**Exit:** Product owner and technical lead approve operational readiness for the selected commercial deployment tiers.

## Global Definition of Done

Every hybrid task must include:

- Automated unit/integration tests where practical.
- Offline and reconnect test if connectivity is involved.
- Tenant/site permission tests.
- Audit trail verification for sensitive operations.
- Documented failure and recovery behaviour.
- No data loss on retry/restart.
- Updated runbook and progress tracker.
