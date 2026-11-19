# 17 — Hybrid Deployment and Subscription Work Package

- **Canonical detailed plan:** [Hybrid Deployment H0–H7](task-plans/HYBRID_DEPLOYMENT_TASK_PLAN.md)
- **Canonical task status:** [Hybrid task registers](../registers/README.md#task-registers)
- **Related decisions:** ADR-011, ADR-012, ADR-013, ADR-014

## Purpose

This is the short entry point for hybrid deployment planning. Detailed tasks, dependencies and acceptance evidence live only in the canonical task plan and machine-readable register.

## Work-package map

| Work package | Scope |
|---|---|
| H0 | Deployment discovery and pilot topology |
| H1 | Cloud Control Plane |
| H2 | Local Data Plane packaging |
| H3 | Signed licence and entitlement enforcement |
| H4 | Secure node-to-cloud synchronization |
| H5 | Backup, updates and local operations |
| H6 | Multi-campus topology |
| H7 | Pilot and hardening |

## Status and change control

Task-level status is maintained in the hybrid CSV partitions. Update the detailed plan and matching CSV row in the same pull request. Do not maintain a second checklist here. Pilot topology, licence grace and customer operating responsibilities remain subject to their ADR and approval gates.
