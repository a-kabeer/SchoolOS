# ADR-011 — Hybrid Cloud Control Plane and Local Data Plane

- **Status:** Accepted as product direction
- **Date:** 9 November 2026
- **Decision owners:** Product owner and technical lead

## Context

The product is intended for private school chains that require broad local operation when internet connectivity is interrupted. A PWA cache alone cannot provide a full local operational backend, and a cloud-only deployment makes local operations dependent on internet access.

## Decision

Use a central SchoolOS Cloud Control Plane with one or more customer Local Data Plane nodes. Each Local Node runs the ASP.NET Core Modular Monolith, React UI, PostgreSQL, local authentication, local files, license verifier and synchronization agent.

The product supports a shared group node and per-campus edge nodes. A per-campus node is required when that campus must continue independently while disconnected from the head office and other campuses.

## Consequences

Positive:
- Local operations continue during internet outages.
- Cloud remains responsible for billing, license issuance, update distribution and sync coordination.
- Customer operational data can remain local by default.
- Per-campus autonomy can be added without changing business modules into microservices.

Trade-offs:
- Installation, updates, monitoring, backups and support become more complex.
- Multi-campus data requires explicit ownership and event synchronization.
- Cloud reports may be stale while a node is disconnected.
- Parent access from outside the school requires an explicit gateway or VPN.
- AI features that depend on cloud model providers will not run during an internet outage unless a local AI runtime is installed.

## Alternatives considered

1. Cloud SaaS + PWA cache only — rejected as insufficient for full local-server offline operation.
2. Fully self-hosted without cloud control plane — rejected as it weakens subscription administration and centralized fleet management.
3. Separate microservices per business module — rejected as unnecessary operational complexity for the initial team.

## Required follow-up

- Implement Cloud Control Plane/local node topology.
- Define customer/site node count and deployment responsibilities.
- Define sync data ownership and conflict rules.
- Define signed offline license lease and its limits.
