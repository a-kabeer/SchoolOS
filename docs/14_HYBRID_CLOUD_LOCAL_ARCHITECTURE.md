# 14 — Hybrid Cloud + Local Deployment Architecture

**Version:** 1.1  
**Status:** Recommended target architecture; deployment topology is configurable per customer.  
**Decision:** SchoolOS will use a central Cloud Control Plane and one or more Local Data Plane nodes. Local school operations must not depend on a live cloud connection.

## 1. Executive decision

SchoolOS is no longer designed as a cloud-only SaaS with a browser cache. The selected deployment is **Hybrid Cloud + Local Server**:

- The **Cloud Control Plane** is operated by SchoolOS. It owns customer onboarding, subscriptions, billing, license issuance, node registry, signed software releases, sync coordination, optional off-site backup catalogue and optional consolidated reporting.
- The **Local Data Plane** is deployed at the school group or campus. It runs the operational ASP.NET Core application, React web UI, PostgreSQL database, local authentication, file storage, background workers and sync agent.
- Staff connect to the local node using the school's LAN. The system continues to support local operations when the internet connection to SchoolOS Cloud is unavailable.
- The local node synchronizes with the cloud when connectivity returns. Cloud synchronization is asynchronous and must never be a prerequisite for a local attendance entry or other approved local workflow.

This architecture creates two deployment planes, not a microservices architecture for every business module. Each Local Data Plane continues to run the same ASP.NET Core Modular Monolith package.

## 2. Important terminology

| Term | Meaning |
|---|---|
| Cloud Control Plane | SchoolOS-operated central service for subscription, licensing, node management, updates and synchronization coordination |
| Local Data Plane | Customer-side deployment that runs day-to-day school operations and stores the operational database |
| Local Node / Edge Node | One deployed SchoolOS Local Data Plane instance at a school group site or campus |
| Tenant | A customer organization / school group in the SaaS commercial model |
| Site | A physical school/campus location where a local node may be installed |
| License lease | A cryptographically signed, time-bounded permission document issued by the Cloud License Service |
| Sync Outbox | Durable local queue of committed events awaiting delivery to the cloud |
| Sync Inbox | Durable record of events received from the cloud or another authorized node |

## 3. Logical architecture

```text
                    SCHOOLOS CLOUD CONTROL PLANE
  +----------------------------------------------------------------+
  | Tenant Registry | Subscription & Billing | License Signer       |
  | Node Registry   | Entitlements            | Signed Release Store |
  | Sync Hub        | Cloud Inbox/Outbox      | Backup Catalogue     |
  | Optional Group Reporting / Parent Portal Read Models            |
  +----------------------------------------------------------------+
                  ^                         ^
                  | outbound mTLS / HTTPS    | outbound mTLS / HTTPS
                  | signed events + leases  | signed updates + commands
                  |                         |
       +--------------------------+  +--------------------------+
       | LOCAL EDGE NODE — A      |  | LOCAL EDGE NODE — B      |
       | Campus A                |  | Campus B                |
       |                          |  |                          |
       | React Web UI / PWA       |  | React Web UI / PWA       |
       | ASP.NET Core Modular     |  | ASP.NET Core Modular     |
       | Monolith                 |  | Monolith                 |
       | PostgreSQL               |  | PostgreSQL               |
       | Local Identity/Auth      |  | Local Identity/Auth      |
       | File/Object Storage      |  | File/Object Storage      |
       | Sync Outbox/Inbox        |  | Sync Outbox/Inbox        |
       | License Verifier         |  | License Verifier         |
       +--------------------------+  +--------------------------+
             ^       ^                    ^       ^
             | LAN   | LAN                | LAN   | LAN
          Staff   Teachers              Staff   Teachers
```

The diagram shows the per-campus edge-node topology. A smaller customer can run one shared local node for several campuses if those campuses can reliably reach it over a private network/VPN. The topology must be selected against the customer's outage requirements, not only hardware cost. If independent local operation is part of the contract for every campus, per-campus nodes are the default.

## 4. Local-node topology options

### Option A — One shared Local Node per school group

- One local server is installed at the group's head office or primary site.
- All campuses connect to it through a private WAN/VPN or a reliable internal network.
- Internet loss between the local node and SchoolOS Cloud does not stop campus operations while the private network and local node remain reachable.
- If a branch loses its connection to the head-office node, that branch cannot use the shared server unless a branch edge node is also deployed.

**Use when:** Campuses are reliably connected to the shared local server and the customer accepts that branch-to-head-office network dependency.

### Option B — One Local Node per campus

- Each campus has its own local server and database.
- Each campus continues independently during internet/WAN outages.
- Nodes exchange authorized data through the Cloud Sync Hub when connectivity is available.
- Group-wide reporting and cross-campus changes may be stale until the relevant events synchronize.
- More hardware, monitoring, backups, installation and sync-conflict management are required.

**Use when:** The commercial promise is that every campus must keep working even when it cannot reach other campuses or the internet.

### Recommended product capability

Support both topologies. For initial pilots, prefer the least complex topology that meets the school's actual outage requirement. If every branch must operate independently during a WAN outage, deploy a Local Node at every campus; a single head-office server cannot satisfy that requirement by itself.

A later high-availability option may use a local server cluster, but that is not required for the first release.

## 5. Responsibilities by plane

### 5.1 Cloud Control Plane

Cloud responsibilities:

- Customer/tenant registration and onboarding.
- Subscription plans, invoices, payment records and renewal status.
- License signing and lease issuance.
- Local-node registration, activation, deactivation and inventory.
- Feature entitlements and capacity limits.
- Signed software release manifests and update channels.
- Sync event relay, node cursors and duplicate-delivery protection.
- Optional encrypted off-site backups and restore catalogue.
- Optional consolidated group reporting from approved projections.
- Optional parent/student remote portal using deliberately replicated, minimum-necessary read models.
- Billing reminders, service notices and support workflow.

Cloud Control Plane should not be required for local attendance, local cash receipt capture, local timetable access or other approved local operations.

### 5.2 Local Data Plane

Each local node responsibilities:

- Serve the React UI over the local network using HTTPS.
- Run the ASP.NET Core Modular Monolith.
- Store authoritative local operational records in PostgreSQL.
- Authenticate authorized school users locally while disconnected.
- Enforce roles, campus scope, subscription entitlements and license validity server-side.
- Store local documents and attachments.
- Run background workers for local workflows.
- Maintain durable Outbox/Inbox and synchronization cursors.
- Verify signed license leases and signed update packages.
- Keep local audit logs and encrypted backups.
- Continue allowed operations without cloud connectivity.

### 5.3 Parent and student access from outside the school

A local-only system is not automatically reachable by parents over the public internet. Do not expose a campus database or local API directly to the internet.

For a remote parent/student portal, use a later, explicit Cloud Portal Gateway:

- Replicate only the minimum fields approved by the school, with last-synchronized timestamps.
- Show when data may be stale because a campus is offline.
- Accept parent requests as pending commands/intents for the Local Node to process.
- Treat online payment-provider notifications as payment-received events awaiting local allocation/ledger acknowledgement; do not claim the local ledger is updated until it confirms.
- Avoid direct cloud writes into local operational tables.

Until this gateway is implemented, remote parent access requires a supported secure access arrangement such as a customer VPN; do not open arbitrary inbound ports on the school's local server.

## 6. Database topology

The earlier single shared operational PostgreSQL database applies to a cloud-only deployment concept. Under the selected hybrid model, use separate database roles for control-plane and operational data:

### Cloud Control Plane database

Contains platform metadata only, such as:

- Tenants and customer account metadata.
- Subscription plans, subscriptions, invoices and payment references.
- Node registry and node credentials metadata.
- Signed-license issuance records and entitlement definitions.
- Release manifests and update rollout records.
- Sync inbox/outbox envelopes, cursors and delivery status.
- Backup catalogue and optional reporting projections.

It should not become an uncontrolled full copy of every local student and finance table. Any replicated school data must have an explicit purpose, minimum scope, retention, access model and customer agreement.

### Local Operational database

Each Local Data Plane has its own PostgreSQL database. A database may contain one tenant's school group and its campuses. A per-campus node stores the site-scoped operational data it owns plus the minimum replicated master data required to work locally.

Use module-owned PostgreSQL schemas (identity, organization, students, fees, finance, attendance, academics, examinations, etc.). Keep `tenant_id`, `school_id`, `campus_id` and `node_id` where applicable, even when a local deployment currently serves one tenant. These fields preserve provenance and simplify support, reporting and future topology changes.

## 7. Data ownership and synchronization boundaries

Do not use generic bidirectional database replication as the business synchronization strategy. Use versioned business events and explicit ownership rules.

| Data category | Suggested owner | Sync approach |
|---|---|---|
| Subscription, plan, license, release | Cloud Control Plane | Signed commands/leases from cloud to local node |
| Tenant/school/campus registry | Cloud/group administration, with local cache | Versioned configuration events |
| User identity and role assignment | Cloud/group admin as master; local auth projection | Replicate authorized account/permission changes; local auth remains available offline |
| Student/guardian master profile | Designated organization or home-campus owner | Versioned updates; conflicts enter review queue |
| Campus attendance | Owning campus node | Append/submit events to cloud hub; immutable source metadata |
| Campus cash collection | Owning campus node | Provisional local capture; server-side validation and ledger posting locally; sync immutable transaction facts |
| Bank statement import/match | Owning finance node | Local authoritative workflow; sync approved results/projections |
| Marks and examination results | Assigned campus/examination authority | Versioned drafts; approval/published result protected from silent overwrite |
| Timetable/curriculum configuration | Designated academic administrator | Versioned configuration replicated to authorized nodes |
| AI jobs/results | Originating node/provider workflow | Sync references and approved outputs according to data policy |
| Audit events | Originating node | Append-only sync; retain origin node, actor and time |

**One-writer rule:** Every mutable aggregate must have a designated owner or an explicit conflict-resolution policy. Avoid allowing two disconnected nodes to silently edit the same student master record or financial ledger.

**Financial sync rule:** Never merge raw journals with generic last-write-wins. Each posted journal has a stable ID, source node, accounting period and immutable posting identity. Group consolidation uses controlled, idempotent postings/projections and duplicate detection.

## 8. Sync protocol

Use a hub-and-spoke synchronization model initially:

1. A local transaction commits its business data and corresponding Outbox event in the same local database transaction.
2. The Local Sync Agent initiates an outbound TLS connection to the Cloud Sync Hub. No public inbound port is required on the school node.
3. Cloud validates node identity, signature, tenant/site scope, event schema and replay/duplicate identifiers.
4. Cloud stores the event in a durable Inbox and acknowledges its receipt.
5. Cloud routes authorized events/configuration commands to subscribed nodes.
6. A receiving node stores the message in its Inbox, validates ownership/version/authorization and applies it idempotently.
7. Business conflicts are recorded for review; they are never silently discarded.
8. The node records a durable cursor and acknowledgement.

Delivery is **at least once**. The desired business effect is exactly-once through idempotent handlers and unique operation/event constraints; do not claim the network itself provides exactly-once delivery.

Minimum event envelope:

```json
{
  "eventId": "stable-unique-id",
  "eventType": "Attendance.Recorded.v1",
  "tenantId": "tenant-id",
  "schoolId": "school-id",
  "campusId": "campus-id",
  "originNodeId": "node-id",
  "aggregateType": "AttendanceSession",
  "aggregateId": "aggregate-id",
  "aggregateVersion": 12,
  "occurredAtUtc": "2026-11-09T08:30:00Z",
  "schemaVersion": 1,
  "payloadReference": "optional-secure-reference",
  "correlationId": "correlation-id"
}
```

The envelope is illustrative, not a final API contract. Avoid putting unnecessary child/student personal data in event metadata. Payloads need schema versioning, authorization and encryption in transit; sensitive payload encryption at rest is also required.

## 9. Network and access design

- Local staff access the Local Node using an internal DNS name and HTTPS.
- Local services bind to the private LAN interface or reverse proxy; PostgreSQL is not exposed to staff devices or the public internet.
- The Sync Agent initiates outbound TLS/mTLS to SchoolOS Cloud.
- Remote support uses a customer-approved, time-limited VPN/zero-trust tunnel; no permanent public admin port.
- If parents need internet access, use the Cloud Portal Gateway described above rather than exposing the local node.
- Use firewall allow-lists, separate server/admin network where feasible, secure Wi-Fi and device policies.
- DNS and TLS certificate lifecycle for local HTTPS must be part of installation/support runbooks.

## 10. Local infrastructure and operations

Each Local Node requires:

- Supported 64-bit Linux host or managed appliance.
- SSD/NVMe storage sized from measured student records, attachments, audit retention and backup needs.
- UPS to handle power interruptions and controlled shutdown; include critical network switch/router/AP power where those devices are required for local access.
- Separate encrypted backup target (NAS/external backup appliance); RAID is not a backup.
- Automatic service restart and health monitoring.
- Disk-space and database-capacity alerts.
- Documented recovery and hardware replacement process.
- Local administrator account and restricted SchoolOS support account.

Exact CPU, memory, disk and backup retention requirements must be measured during pilot load tests. Do not promise that a low-cost PC will support every campus size without benchmarking.

## 11. AI availability in hybrid/local mode

- Cloud AI providers require an internet connection. Those AI functions will be unavailable or queued while the site has no internet.
- Core school workflows must remain usable without AI.
- A later optional Local AI Pack may run selected speech/OCR/LLM models on customer hardware, but this creates extra hardware, maintenance, model-update, privacy and licensing requirements.
- Do not describe the whole AI suite as offline-capable until each model and device requirement has been tested locally.

## 12. Failure behaviour

| Failure | Required behaviour |
|---|---|
| Internet/Cloud unavailable | Local operations continue; sync queues locally |
| Cloud license endpoint unavailable | Existing valid signed lease continues until its signed expiry/grace boundary |
| Local node down | Site operations stop until node is restored or a tested failover node is used |
| Campus WAN down, shared-node topology | Affected branch cannot reach shared node; independent edge node is needed for autonomy |
| Sync event rejected | Preserve event and expose a review/error state; do not drop it |
| Disk almost full | Alert administrators; restrict risky new attachment growth before database corruption risk |
| Power loss | UPS and database recovery; verify backups |
| Cloud reporting stale | Show last sync time and stale-data indicator |

## 13. Acceptance criteria

- Disconnect internet for a defined test window; local attendance and other approved workflows continue.
- Restart Local Node containers; PostgreSQL data, ASP.NET Data Protection keys and license state survive.
- Restore Local Node from backup to replacement hardware using documented steps.
- Repeated sync delivery does not duplicate attendance, payment or journal effects.
- Two nodes making conflicting edits create explicit conflict records.
- The Cloud Control Plane cannot directly mutate local operational tables.
- Local database is not reachable from the public internet.
- Node credential revocation takes effect on reconnection; offline revocation limitations are documented.
- Stale cloud read models visibly show last-synchronized time.
