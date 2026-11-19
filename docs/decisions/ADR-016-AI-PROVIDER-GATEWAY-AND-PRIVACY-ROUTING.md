# ADR-016 — Central AI Provider Gateway and Privacy-Aware Routing

- **Document ID:** ADR-016
- **Status:** Accepted
- **Decision maturity:** Accepted direction
- **Owner:** Technical lead (accountable role; named assignee TBD)
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `8356f248e54d317294c4972d124e26ecc6ae84d9` (base for documentation governance refactor)
- **Date:** 9 November 2026

## Context

Multiple AI capabilities may use different providers and modalities. Direct provider calls from React or individual feature modules would duplicate credentials, routing, cost, privacy and reliability controls.

## Decision

All AI inference and related provider operations go through a backend Provider Gateway. The gateway applies tenant-aware routing, capability checks, data classification, minimization, policy, quota, metering, audit, timeout and fallback controls. Fallback is allowed only to a provider/runtime satisfying the same or stricter privacy requirements.

## Consequences

- API keys remain server-side.
- Provider adapters are isolated from business policy.
- A .NET AI abstraction may be evaluated, but SchoolOS retains control of routing and policy.
- Current provider retention/training/region terms must be checked before enabling a route.
- “Not used for training” is not equivalent to zero retention.

## Follow-up

Choose providers and adapter contracts after benchmark and contract review. Define a secrets manager, retention schedule, data-processing agreements and test matrix.
