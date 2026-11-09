# ADR-022 — Primary Identifier Strategy (UUIDv7)

- **Status:** Accepted
- **Date:** 2026-11-09
- **Owner:** Software Architecture Team
- **Reviewers:** Technical Lead, Senior Backend Architect

## Context

SchoolOS requires stable, globally unique identifiers across all domain entities in both the Cloud Control Plane and Local Data Plane nodes. In hybrid multi-campus school management, records (such as attendance, marks, fees, and students) are generated locally at edge nodes and subsequently synchronized via event envelopes to the Cloud Control Plane or regional hubs.

The blueprint (`docs/04_DATABASE_BLUEPRINT.md` §3) previously noted: *"stable primary identifier (UUID recommended, final strategy to be set)"*. This ADR formally resolves that open decision.

## Decision Drivers

1. **Distributed & Offline Generation:** Local Data Plane nodes must generate permanent, collision-resistant primary keys offline without coordinating with a central database sequence or network authority.
2. **PostgreSQL B-Tree Index Performance:** Tables such as attendance records, fee invoices, journal lines, and audit logs will hold millions of records. Random UUIDv4 keys cause severe B-tree page splits, index fragmentation, cache eviction, and write amplification.
3. **Database Portability & Standard Types:** Identifiers must map natively to PostgreSQL `uuid` (16 bytes) without custom string or bigint hacks.
4. **.NET 10 Native Support:** .NET 9+ and .NET 10 provide `Guid.CreateVersion7()` natively in `System.Guid` with high throughput and zero external dependencies.

## Options Considered

1. **Auto-incrementing BigInt (IDENTITY / Sequence):**
   - *Pros:* Compact (8 bytes), sequential, optimal B-tree locality.
   - *Cons:* Fails completely in disconnected multi-campus edge nodes. Requires central allocation or complex range partitioning to prevent key collisions during multi-node synchronization. Unsuitable for hybrid offline-first architecture.
2. **Random UUIDv4 (`Guid.NewGuid()`):**
   - *Pros:* Fully distributed, zero collision risk, supported everywhere.
   - *Cons:* Complete lack of locality. In large tables, index inserts hit random B-tree pages, forcing cold disk reads and degrading write throughput under high concurrency.
3. **UUIDv7 (RFC 9562) via `Guid.CreateVersion7()` (Selected):**
   - *Pros:* Combines a 48-bit millisecond timestamp prefix with high-entropy randomness. Sequential when generated over time, ensuring contiguous B-tree page appends in PostgreSQL while remaining globally unique across distributed nodes. Native in .NET 10.
   - *Cons:* Minor theoretical leakage of record creation timestamp (negligible for educational ERP entities; audit timestamps are already public within tenant boundaries).

## Decision

We adopt **UUIDv7** as the default primary identifier strategy for all domain entities across SchoolOS.

1. **Entity Generation:** New entities generate their ID via `Guid.CreateVersion7()` upon instantiation or factory creation.
2. **Database Schema:** PostgreSQL columns use the native `uuid` type with primary key B-tree indexes.
3. **API & Contracts:** Exposed as standard 36-character hexadecimal UUID strings (`xxxxxxxx-xxxx-7xxx-xxxx-xxxxxxxxxxxx`).

## Consequences

### Positive
- Fully collision-resistant key generation across independent campus local nodes during offline operations.
- Preserves PostgreSQL index clustering locality, minimizing B-tree page splits and index bloat.
- Eliminates key re-mapping during offline event synchronization to the Cloud Control Plane.
- Native runtime support via `Guid.CreateVersion7()` with zero third-party library overhead.

### Negative / Trade-offs
- Keys are 16 bytes compared to 8-byte bigints (offset by modern SSD storage and PostgreSQL index efficiency).
- Primary key values sort chronologically by default; applications must not rely on UUID ordering for strict business domain sorting (explicit `created_at_utc` or domain sequence columns remain mandatory).

## Security / Privacy Impact
- UUIDv7 encodes timestamp precision to the millisecond. This is safe for school operational records as creation dates are standard business metadata. External-facing sensitive tokens (e.g. password reset tokens, license signatures) do not use entity IDs.

## Operational Impact
- Zero database extensions required (PostgreSQL stores standard 128-bit UUIDs natively).
- Backup, restore, and data exchange tools treat UUIDv7 as standard UUID types.

## Follow-up Tasks
- [x] Configure base `Entity<Guid>` to use `Guid.CreateVersion7()` in factory/creation methods.
- [x] Map EF Core entity key configurations to native PostgreSQL `uuid`.

## References
- RFC 9562: *Universally Unique IDentifiers (UUIDs)* (superseding RFC 4122)
- `docs/04_DATABASE_BLUEPRINT.md` §3
- Microsoft .NET 9/10 `Guid.CreateVersion7` documentation
