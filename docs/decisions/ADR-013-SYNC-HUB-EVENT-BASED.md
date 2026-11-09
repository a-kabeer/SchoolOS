# ADR-013 — Event-Based Hub-and-Spoke Synchronization

- **Status:** Accepted as design direction
- **Date:** 9 November 2026
- **Decision owners:** Product owner and technical lead

## Context

Multiple local nodes may operate independently. Direct database replication can create unsafe writes, schema coupling and financial conflicts. A business-aware synchronization strategy is required.

## Decision

Use local transactional Outbox/Inbox, a Cloud Sync Hub, versioned events, per-node cursors and idempotent handlers. Delivery is at least once; exactly-once business effects are achieved through durable idempotency constraints and replay-safe handlers.

Every mutable aggregate must have a designated owner or explicit conflict policy. Financial journals are immutable after posting and are not merged by last-write-wins.

## Consequences

- Local operations are durable during cloud outages.
- Sync can be retried and monitored.
- Group-wide data may be eventually consistent and must show last-sync time.
- Conflict resolution and ownership rules require careful product design.
