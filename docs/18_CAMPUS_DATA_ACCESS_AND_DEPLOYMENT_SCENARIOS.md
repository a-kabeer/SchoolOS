# 18 — Campus Data Access and Deployment Scenarios

**Version:** 1.2  
**Status:** Product/architecture design captured; exact release sequencing and customer topology remain to be validated.  
**Context:** One main campus plus five additional campuses. The additional campuses may be disconnected from one another and from the main campus.

## 1. Executive summary

SchoolOS should support three deployment profiles that share the same business modules, campus identity model, permissions and data-export contracts:

1. **Disconnected Local + Manual Data Exchange:** Every campus runs its own Local Node and operational database. No network connection between campuses is assumed. Each additional campus exports an encrypted, signed, versioned data package which an authorized operator imports into the main-campus/head-office instance. The headmaster can view each campus separately and see consolidated imported snapshots. This data is not real-time.
2. **Central Cloud Application:** All campuses use one central cloud-hosted SchoolOS application. A shared tenant database (or logically isolated tenant store) records campus scope on relevant data. The headmaster has organization-wide permissions; campus staff have only their assigned campus permissions. Campus information is available centrally as soon as users save it, subject to connectivity and service availability.
3. **Hybrid Local Nodes + Cloud Sync:** Each campus or group site runs a Local Node. Nodes operate independently during internet outages and synchronize approved events/projections through the Cloud Sync Hub when connectivity returns. If all campuses must operate independently when disconnected from each other, each needs its own Local Node.

**Recommended product direction:** Keep the selected Hybrid Cloud Control Plane + Local Data Plane architecture as the strategic baseline. Add a controlled manual Data Exchange capability so fully disconnected campus deployments can consolidate data at the main campus. Also document a Cloud Centralized deployment profile for customers with reliable internet. These are deployment profiles, not separate school-business products.

## 2. Scenario 1 — Self-hosted local application with disconnected campuses

### 2.1 Topology

- Main campus runs its own SchoolOS Local Node and database.
- Each of the five additional campuses runs its own Local Node and database.
- No LAN/WAN connectivity between campuses is assumed.
- A campus continues its own local workflows without internet or access to other campuses, subject to its local hardware and valid signed license lease.
- Data moves to the main campus only through an authorized transfer process, such as encrypted USB/media or another approved secure channel.

```text
Campus 2 Local Node ---- encrypted export package ----+
Campus 3 Local Node ---- encrypted export package ----+
Campus 4 Local Node ---- encrypted export package ----+--> Main Campus Node
Campus 5 Local Node ---- encrypted export package ----+    Headmaster dashboard
Campus 6 Local Node ---- encrypted export package ----+    campus-wise views
Main Campus Local DB ----------------------------------+    consolidated snapshots
```

### 2.2 Export workflow at an additional campus

Provide a dedicated menu:

`Data Management → Export Campus Data`

An authorized campus administrator selects:

- Campus and source node (pre-bound; user cannot freely change identity).
- Export type: incremental changes since last acknowledged export, date range, or controlled full snapshot.
- Data domains: admissions, student/guardian records, attendance, fee transaction facts, examination results, staffing or other approved data.
- Attachments/document inclusion, only where permitted and necessary.
- Export reason and intended receiving organization/node.

The Local Node creates a **SchoolOS Data Exchange Package**, not a PostgreSQL database dump. The package should include a manifest, package ID, tenant/school/campus/node IDs, export cursor/watermark, schema and module versions, generated time, record counts, checksums, event IDs, dependency references and a signed manifest. Sensitive payloads must be encrypted for the intended receiver. A package may be transferred via encrypted removable media or an approved secure file-transfer route.

### 2.3 Import workflow at the main campus

Provide a dedicated menu:

`Campus Management → Data Exchange → Import Package`

The main-campus operator:

1. Selects the incoming package.
2. Verifies signature, recipient, tenant, source node, package integrity and schema compatibility.
3. Runs a dry-run validation and shows a human-readable summary.
4. Checks duplicate package/event IDs and already imported watermarks.
5. Checks references, campus ownership, academic-year mappings and data constraints.
6. Shows creates, updates, skipped items, rejected items and conflicts separately.
7. Requires an authorized approver for the import.
8. Applies the import transactionally in bounded batches with a durable import log and rollback/recovery procedure.
9. Records the package ID and acknowledgement so an identical package cannot be applied twice.

Do not allow a package to overwrite main-campus records simply because IDs or names happen to match. Imported campus data must retain its originating campus and node provenance.

### 2.4 Headmaster access

The main-campus application provides a dedicated **Head Office / Campus Overview** option. The headmaster can:

- Select “All Campuses” for consolidated reports based on the latest imported data from each branch.
- Select one campus for campus-specific student, attendance, fee, admission, staff and examination views.
- See each campus's last export/import time, package status, exported watermark and stale-data indicator.
- Compare campuses using the latest available imported snapshots.
- Access permitted imported records according to organization-level and campus-level permissions.

By default, imported branch data is a **read-only reporting replica** at the main campus. The main campus must not silently edit branch-owned operational records. If central corrections are required, use a versioned correction request/command that is exported back to the source campus and applied there by an authorized user. A true round-trip workflow is a separate feature and requires conflict handling and acknowledgement.

### 2.5 What this scenario can and cannot provide

**Can provide:** Independent local operations; no inter-campus network required; campus-wise views at head office after import; consolidated reports from imported data; controlled data transfer and audit.

**Cannot provide:** Real-time branch data; central changes immediately appearing at disconnected branches; reliable direct edits to remote branch records; a guarantee that the headmaster's dashboard is current when a campus has not exported recently.

The UI must always show “Last imported” and a freshness indicator. Do not label imported snapshots as live data.

## 3. Scenario 2 — Central cloud-hosted application

### 3.1 Topology

```text
                    SchoolOS Cloud Application
              ASP.NET Core + React + PostgreSQL
                              |
        +---------------------+----------------------+
        |                     |                      |
   Main Campus             Campus 2               Campus 3 ... 6
   user sessions           user sessions          user sessions
```

All campuses use the same cloud-hosted application and organization tenant. Relevant business records contain server-enforced `tenant_id`, `school_id` and `campus_id` scope. A campus may be represented by an application-level campus scope; it does not require a separate database per campus in this profile.

### 3.2 Headmaster and campus access

- Headmaster is granted organization-wide campus permissions and can select any authorized campus or “All Campuses”.
- A campus principal is restricted to the assigned campus or campuses.
- An accountant may have finance permissions for one or more explicitly assigned campuses.
- A teacher may access only assigned classes, subjects and students.
- Access is enforced in backend authorization and database query/RLS safeguards, not just by hiding UI menu items.
- Each campus manages its own operational records and settings within organization policy.

### 3.3 Benefits and limitations

**Benefits:** Near-real-time centralized visibility; no manual import/export; simpler centralized updates and backups; easier subscription enforcement; easier consolidated reporting.

**Limitations:** Campus operations require internet connectivity to reach the cloud unless a local edge/offline node or separately designed offline workflow is deployed. A PWA cache alone does not make the full backend available offline. Cloud outages also affect all connected campuses unless appropriate availability architecture is provided.

### 3.4 Data isolation

The default cloud profile can use shared PostgreSQL infrastructure with tenant- and campus-scoped rows, supported by strict server-side authorization and optionally PostgreSQL Row-Level Security. For larger customers, a separate database or database instance can be offered as a premium isolation option, but it is not required for each campus by default.

## 4. Scenario 3 — Hybrid Local Nodes with automatic cloud synchronization

This remains the strategic deployment baseline selected for SchoolOS.

- Each campus that must operate independently during WAN/internet outages receives a Local Node.
- Alternatively, multiple campuses may share a group node if a reliable private network connects them and the customer accepts that dependency.
- Each node owns the operational records assigned to its campus/site.
- Local transactions commit to the local PostgreSQL database and transactional Outbox.
- A local Sync Agent initiates outbound authenticated connections to the Cloud Sync Hub.
- The Cloud Sync Hub stores and routes authorized events/projections to the group reporting view and other eligible nodes.
- The headmaster sees consolidated data in cloud reporting, with last-sync timestamps and stale-data warnings.
- Conflicting edits are routed to explicit review; financial records are never merged using generic last-write-wins.

If cloud internet is unavailable, campus local operations continue. Campus-to-campus reporting and cloud consolidation may be stale until synchronization resumes.

## 5. Side-by-side comparison

| Criterion | Scenario 1: Disconnected manual exchange | Scenario 2: Cloud centralized | Scenario 3: Hybrid local + cloud sync |
|---|---|---|---|
| Independent local operation | Yes | Not by default | Yes, where local nodes exist |
| Internet needed for daily campus operations | No | Yes | No for approved local workflows |
| Headmaster access to other campuses | After import | Near real time | After sync; currentness shown |
| Manual package transfer | Required | No | Fallback/recovery option |
| Automatic data consolidation | No | Yes | Yes, when connected |
| Central subscription enforcement | Periodic signed license | Direct server-side | Signed local lease + cloud check-ins |
| Hardware at each campus | Yes | No | Yes for independent edge nodes |
| Operational complexity | Import/export and local support | Central cloud operations | Highest: local nodes + cloud sync |
| Best fit | Disconnected sites with no dependable WAN | Reliable internet | Chains requiring local autonomy and central visibility |

## 6. Shared product requirements across all scenarios

1. Use the same SchoolOS business modules and campus identity model across profiles.
2. Give every campus a stable immutable identifier; names and codes are not sufficient identity keys.
3. Preserve source node, source campus, actor, event time and import/sync provenance.
4. Maintain organization-level and campus-level roles/permissions.
5. Give the headmaster a dedicated campus selector and “All Campuses” reporting view.
6. Display data freshness: live, last synchronized, last imported or stale.
7. Use stable globally unique IDs (for example UUIDs) across nodes to prevent collisions.
8. Version export/import package schemas and module event contracts.
9. Make imports idempotent and replay-safe.
10. Provide dry-run, approval, rejection, partial-failure and recovery reporting.
11. Never import arbitrary database dumps from untrusted media.
12. Keep financial postings immutable and ensure imported financial event identities cannot create duplicate receipts or journal entries.
13. Ensure imported data is scoped to the intended tenant and campus.
14. Encrypt packages containing student, guardian, staff or financial information; verify signed manifests before processing.
15. Apply retention, consent, customer authorization and data minimization rules to every data transfer.

## 7. Proposed data-exchange entities

These are candidate tables in a module-owned schema; exact columns and constraints must be designed before implementation.

| Table | Purpose |
|---|---|
| `exchange.export_batches` | Export batch header and source watermark |
| `exchange.export_items` | Records/events included in a batch |
| `exchange.package_manifests` | Package ID, schema version, checksums and signature metadata |
| `exchange.import_batches` | Import attempt, receiver, status and approver |
| `exchange.import_items` | Per-item accepted/skipped/rejected/conflict state |
| `exchange.import_cursors` | Last successfully imported source watermark per node/domain |
| `exchange.conflicts` | Version/ownership conflicts needing review |
| `exchange.acknowledgements` | Import result returned to source campus |
| `exchange.transfer_keys` | Public-key/certificate metadata for approved transfer endpoints |

Avoid storing private signing keys in the database as ordinary rows. Key material belongs in an appropriate protected key store or managed device security boundary.

## 8. Proposed package manifest

Illustrative only; this is not a final wire contract:

```json
{
  "packageId": "globally-unique-id",
  "tenantId": "tenant-id",
  "schoolId": "school-id",
  "campusId": "campus-id",
  "sourceNodeId": "node-id",
  "destinationNodeId": "main-campus-node-id",
  "packageType": "IncrementalEvents",
  "createdAtUtc": "2026-11-09T10:00:00Z",
  "schemaVersion": 1,
  "moduleVersions": {
    "Students": 1,
    "Attendance": 1,
    "Fees": 1
  },
  "sourceWatermark": "opaque-cursor",
  "recordCounts": {
    "students": 12,
    "attendanceEvents": 240,
    "paymentEvents": 8
  },
  "contentHash": "sha256-hash",
  "signatureKeyId": "campus-signing-key-01"
}
```

The actual package must be cryptographically signed and encrypted to an intended recipient. Manifest metadata must not unnecessarily expose student names or sensitive personal data.

## 9. Data authority and edit policy

The product must distinguish **viewing replicated data** from **owning and editing operational data**.

- In Scenario 1, source campus remains authoritative for campus-owned records. Main-campus imports are reporting replicas by default.
- In Scenario 2, the central cloud database is authoritative; campus scoping controls which users may modify which records.
- In Scenario 3, each Local Node is authoritative for its owned campus aggregates; cloud contains sync state and approved reporting projections. A designated owner is required for shared group entities.
- Student/guardian transfer between campuses must be a formal transfer workflow, not a blind duplicate import.
- A main-campus correction to a disconnected campus record must be represented as a correction request/command and applied by the source node, or handled through an explicitly approved ownership transfer.
- Posted financial entries must never be overwritten during import; use source-linked, idempotent posting/reversal workflows.

## 10. Security and operational controls

- Use authenticated and authorized exports/imports.
- Encrypt transfer packages using recipient-bound encryption and sign manifests with protected signing keys.
- Verify package tenant, campus, source node and destination before processing.
- Reject unsupported schema versions; do not attempt a best-effort destructive import.
- Keep package files in restricted storage and delete/archive them according to an approved retention policy after confirmed import.
- Record export, transfer, import, approval, rejection and re-import attempts in immutable audit history.
- Malware-scan attachments and validate file types and size limits.
- Do not auto-run scripts, SQL or executable content from a package.
- Require backups before material imports and maintain a tested rollback/recovery process.
- A fully disconnected local node cannot receive immediate remote license revocation; it enforces the last valid signed lease.

## 11. Recommended implementation order

1. **Cloud centralized profile:** implement campus selector, organization-wide headmaster access and campus-scoped permissions using the existing cloud/control-plane patterns. This is the simplest centralized reporting model where connectivity is reliable.
2. **Manual Data Exchange:** implement signed/encrypted incremental package export/import, dry-run validation, deduplication, import logs, read-only campus reporting and freshness labels. This is the fallback for fully disconnected self-hosted campuses.
3. **Hybrid automatic sync:** reuse the same event contracts, ownership rules, package versioning and import validation concepts for automatic node-to-cloud synchronization. Do not create a second, incompatible data model.

The strategic hybrid architecture remains the target for customers that require independent local operation and centralized reporting. Exact order may change based on pilot customer demand and infrastructure budget.

## 12. Acceptance criteria

### Scenario 1
- Six disconnected Local Nodes can operate independently.
- Each branch can export an incremental package for its own campus.
- Main campus can validate and import packages from five branches without touching its own records.
- Re-importing the same package does not duplicate students, attendance events, receipts or journal effects.
- Headmaster can view each campus separately and consolidated imported reports.
- Every imported view shows the last imported timestamp and freshness state.
- Branch-owned records are read-only in the main-campus replica unless a formal command/ownership workflow is used.
- Invalid signature, wrong tenant, wrong destination and unsupported schema packages are rejected safely.

### Scenario 2
- Headmaster can access all six campuses from one login.
- Campus principals and staff can access only assigned campus scopes.
- The backend and database safeguards prevent cross-campus and cross-tenant access.
- Saved changes appear in central reporting without a manual transfer.
- Audit logs record campus, user and action.

### Scenario 3
- Each independently operating campus has a Local Node.
- Local operations continue during a defined internet outage.
- Sync resumes after connection returns and does not duplicate business effects.
- Headmaster group reports show stale-data state when a campus is offline.
- Cross-campus conflicts and transfers follow explicit workflows.

## 13. Open decisions

- Which profile will be the first commercial pilot: cloud centralized, disconnected manual exchange, or hybrid automatic sync?
- Will manual exchange ship in the first production release or as a subsequent capability?
- Which exact data domains are permitted in a manual export package?
- Will a main-campus headmaster only view branch records, or can selected correction commands be sent back?
- How frequently will disconnected campuses be expected to export packages?
- Who transports and safeguards USB/removable media?
- What package retention and encryption-key rotation policy is required?
- Is the main campus itself a branch Local Node, a head-office reporting node, or both logical roles?
- What is the approved conflict and ownership workflow for student/guardian transfers?
- Are remote parent/student portals required when a campus is disconnected?
