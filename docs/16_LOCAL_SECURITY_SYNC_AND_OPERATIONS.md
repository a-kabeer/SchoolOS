# 16 — Local Node Security, Synchronization and Operations Runbook

**Version:** 1.1  
**Status:** Required implementation/runbook baseline for Hybrid deployments.

## 1. Security boundaries

Treat each Local Node as a customer-controlled or managed security boundary. A compromised campus server can expose the operational data stored on it. Protect the host, database, secrets, network and backups together.

## 2. Network security

- Local React UI and ASP.NET API are reachable only from the permitted school LAN/VPN.
- PostgreSQL listens only on the local host/private application network; never expose its port to the public internet or ordinary staff VLAN.
- Cloud Sync Agent initiates outbound HTTPS/mTLS connections to the Cloud Sync Hub.
- No default inbound cloud-to-school connection and no permanent public admin port.
- Remote support requires customer-approved time-limited VPN/zero-trust access, MFA and session logging.
- Separate server/admin network where feasible.
- Use firewall rules, internal DNS and HTTPS certificates for the local application.
- Keep management ports restricted to named administrators.

## 3. Host and container hardening

- Use a supported Linux distribution with automatic security-update process and controlled maintenance windows.
- Run containers as non-root where the software supports it; minimize Linux capabilities and mounted host paths.
- Pin container image versions/digests; do not run unreviewed `latest` images in production.
- Use read-only container filesystems where practical and persistent volumes only for required state.
- Keep secrets out of image layers, Git, environment dumps and diagnostic bundles.
- Persist database files, document storage, license state, ASP.NET Data Protection key ring and sync queues outside ephemeral container layers.
- Protect the ASP.NET Data Protection key ring with restrictive file permissions and encryption at rest; Microsoft's documentation specifically recommends persistent storage such as a Docker volume or external provider for container deployments. See `https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview`.

## 4. Local identity and permissions

Local authentication must work when internet/cloud identity services are unavailable.

- Provision local user accounts and permission snapshots before an outage.
- Store credentials using a reviewed ASP.NET Core Identity configuration or an approved local identity service.
- Require MFA when connected for privileged remote/cloud administration where practical; define a secure offline recovery method for local school admins.
- Local node validates user, role, campus scope and current local entitlement for every protected operation.
- Cloud role revocations and user changes reach the node on sync. The maximum offline revocation delay must be documented and accepted by the customer.
- Device loss or user departure requires local admin revocation at the site when the cloud cannot reach it.
- A disconnected node cannot receive an immediate remote session/user/device revocation.

## 5. Local data protection

- Encrypt the host disk or database/backup volumes using a managed key strategy supported by the chosen OS/platform.
- Encrypt backup archives separately from the live database.
- Store local document files outside the public web root.
- Use least-privilege database roles; the app must not connect as PostgreSQL superuser or table owner with unrestricted RLS bypass.
- Use PostgreSQL Row-Level Security only as defence in depth. Ensure runtime roles do not have `BYPASSRLS`; understand that superusers and `BYPASSRLS` roles bypass row security and table owners normally bypass it unless forced. Official reference: `https://www.postgresql.org/docs/current/ddl-rowsecurity.html`.
- Keep an immutable/append-oriented audit trail for sensitive operations and protected exports.
- Avoid caching unnecessary child data on individual browsers.

## 6. Sync security

- Each node receives a unique cryptographic identity during activation.
- Authenticate with mTLS or an equivalent per-node credential and rotate/revoke credentials.
- Sign or authenticate event envelopes; include event ID, origin node, tenant/site scope, schema version and aggregate version.
- Encrypt all traffic in transit.
- Apply replay protection, idempotency and per-node rate limits.
- Validate event ownership and schema before applying.
- Never trust tenant/site/user identifiers supplied solely by the client payload.
- Separate the Cloud Control Plane's sync credentials from database administrator credentials.
- Store rejected/conflicting events durably for authorized review.

## 7. Backup design

A hybrid customer requires at least two independent backup locations:

1. Local scheduled database and document backups to a separate local storage target/NAS.
2. Encrypted off-site backup to SchoolOS Cloud or another approved customer-controlled destination when internet is available.

Requirements:

- Document RPO/RTO per commercial plan.
- Encrypt before off-site transfer; keep encryption keys separate from the backup files.
- Verify backup integrity and completeness.
- Test restoration to replacement hardware.
- Include PostgreSQL data, uploaded documents, configuration, license state needed for recovery, signing trust bundle and sync queues/cursors.
- Do not restore a stale database over a newer node without a recovery/merge plan.
- RAID, mirrored disks and snapshots are not substitutes for independent backups.
- Alert on backup age, failure, capacity and last successful restore drill.

## 8. Signed updates and upgrade process

- Cloud publishes a signed release manifest containing version, supported database schema range, image digests, release notes and migration requirement.
- Local Update Agent downloads the package outbound and verifies signature and digest.
- Customer selects an approved maintenance window, or plan explicitly authorizes managed unattended updates.
- Local node performs pre-upgrade health checks and a backup.
- Database migrations run once with a durable migration lock and recorded result.
- Health checks run after update.
- Failed application update can roll back the image only if database compatibility is preserved; database rollback requires a tested restore strategy and must not be treated as automatic container rollback.
- Stagger releases to pilot nodes before production fleet rollout.

## 9. Monitoring and diagnostics

Local node should expose health data for:

- API health and version.
- PostgreSQL health, connections and disk use.
- Sync queue depth, oldest pending event and last successful sync.
- License status and lease expiry.
- Backup last-success timestamp.
- File storage capacity.
- CPU/memory and service restarts.
- Update version and migration status.

Diagnostics uploaded to cloud must redact passwords, secrets, private keys and unnecessary child information.

## 10. Incident response

Define runbooks for:

- Lost/stolen local server.
- Suspected customer administrator compromise.
- Ransomware/malware on the school LAN.
- Failed/partial sync.
- Duplicate financial events.
- Disk failure or full disk.
- Corrupt database.
- License-key compromise.
- Signing-key rotation/compromise.
- Failed update/migration.
- Offline node that has not checked in for a long period.

For each incident identify customer contact, SchoolOS responder, containment steps, recovery source, data-loss window, notification obligation and closure evidence.

## 11. Operational acceptance tests

- Internet outage does not interrupt approved local workflows.
- Node restart preserves database, uploaded files, sync queue, license snapshot and Data Protection keys.
- PostgreSQL is unreachable from an ordinary staff device and from the public internet.
- Cloud cannot initiate a direct shell/DB connection to a customer node by default.
- Sync credential rotation works without losing queued events.
- Off-site backup is restored on a clean replacement host.
- A signed update with invalid signature is rejected.
- Disk-full and power-loss procedures are tested.
- Privileged remote support session requires approval and is audited.
