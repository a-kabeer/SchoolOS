# ADR-015 — Hybrid AI Hosting and Optional Offline Inference

- **Status:** Accepted direction; deployment details pending
- **Date:** 9 November 2026

## Context

SchoolOS is designed for hybrid cloud/local school operations. AI must support data privacy choices and must not become a dependency for core offline school workflows.

## Decision

Support three AI hosting modes: Cloud AI through approved external APIs, Private AI infrastructure for premium isolation requirements, and optional Campus Local AI for selected on-premises/offline inference. The product privacy posture is hybrid. Local inference is an optional premium capability and is not mandatory for every campus node.

## Consequences

- A central Provider Gateway is required.
- Each task must be routed according to data classification and provider eligibility.
- Cloud unavailability may make cloud AI unavailable; core school operations continue.
- Local AI availability depends on model support, hardware, updates and local RAG.
- Private/local options have additional operational and support costs.
- The system must not silently move local-only sensitive data to cloud on fallback.

## Alternatives considered

- Cloud-only AI: simpler initially but insufficient for sensitive/local-only workflows and offline inference expectations.
- Mandatory on-premises AI at every campus: cost and hardware burden is unjustified before demand is proven.
- Separate microservices for all AI features: not required initially; use the Modular Monolith with isolated inference processes where needed.

## Follow-up

Define supported data classes, provider contracts, hardware profiles, local model lifecycle, private-hosting SLA and exact customer entitlements.
