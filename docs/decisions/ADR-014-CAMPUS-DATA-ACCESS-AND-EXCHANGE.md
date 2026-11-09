# ADR-014 — Campus Data Access and Disconnected Data Exchange

**Status:** Accepted as a product/architecture direction; exact release order remains open.  
**Date:** 9 November 2026  
**Related:** ADR-011 Hybrid Cloud + Local, ADR-013 Event-Based Synchronization, `docs/18_CAMPUS_DATA_ACCESS_AND_DEPLOYMENT_SCENARIOS.md`.

## Context

A target customer may operate one main campus and five additional campuses that are not connected to each other. The product must also support customers with reliable internet who prefer a single cloud-hosted application, and chains that require independent local operation plus centralized reporting.

## Decision

SchoolOS will define three deployment profiles using a shared campus identity, authorization and business-data contract:

1. **Disconnected Local + Manual Data Exchange:** Each campus has its own Local Node. Branches export signed/encrypted, versioned, incremental data packages. The main-campus node validates and imports them into a campus-scoped reporting replica. The headmaster can view each campus separately and consolidated imported data. The dashboard must show import freshness. Branch-owned imported records are read-only by default.
2. **Cloud Centralized:** All campuses use one cloud-hosted application. A headmaster has organization-wide permissions; campus staff are restricted to their assigned campus scope. The cloud database is authoritative and saved changes are centrally visible without manual import.
3. **Hybrid Local + Cloud Sync:** Local Nodes own campus operational data and synchronize events/projections through the Cloud Sync Hub when connected. Each independently operating campus needs its own Local Node.

The strategic architecture remains Hybrid Cloud Control Plane + Local Data Plane. Manual package exchange is an explicit fallback/compatibility capability for disconnected deployments. Cloud Centralized is a deployment profile for customers with reliable internet.

## Consequences

### Positive
- Supports disconnected campuses without requiring campus-to-campus networking.
- Gives head office campus-wise and consolidated visibility after imports.
- Supports real-time central visibility for cloud customers.
- Reuses stable IDs, event contracts, ownership rules and sync validation across manual and automatic transfer.
- Avoids unsafe raw database-dump imports and blind bidirectional edits.

### Negative / trade-offs
- Manual exchange produces snapshots, not real-time reporting.
- Package signing, encryption, schema compatibility, dry-run validation, import logs and recovery increase implementation scope.
- Campus-owned data edits at head office require command/approval/round-trip workflows; direct editing of an imported replica is disallowed by default.
- Cloud centralized deployment requires reliable connectivity for campus operations unless a local node/offline architecture is also deployed.
- Hybrid local nodes increase hardware, backup, support and operations cost.

## Required controls

- Stable tenant, school, campus and source-node identifiers.
- Globally unique record/event/package identifiers.
- Signed manifest, recipient-bound encryption and schema versioning.
- Import dry-run, duplicate/replay prevention and transactional/batch recovery.
- Explicit source-of-truth and one-writer rules per aggregate.
- Campus-scoped permissions and audit trails.
- Data freshness timestamps on imported/synchronized views.
- Financial records imported idempotently; no generic last-write-wins or overwrite of posted ledger entries.

## Not decided by this ADR

- First commercial pilot profile and release sequence.
- Manual export/import frequency.
- Exact data domains included in packages.
- Main-campus correction/command permissions.
- Removable-media operational policy and retention period.
- Final customer pricing and hardware bundle.
