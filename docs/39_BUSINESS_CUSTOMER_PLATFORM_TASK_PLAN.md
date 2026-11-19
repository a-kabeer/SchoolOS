# 39 — Business & Customer Platform Implementation Task Plan

- **Canonical detailed plan:** [BCP Implementation Plan](task-plans/BCP_IMPLEMENTATION_TASK_PLAN.md)
- **Canonical task status:** [BCP task register](../registers/task-register-business-customer-platform.csv)
- **Related decision:** [ADR-020 — Business & Customer Platform](decisions/ADR-020-BUSINESS-CUSTOMER-PLATFORM.md)

## Purpose

This is the concise delivery entry point for BCP. Detailed BCP-0–BCP-11 work packages, BCP-001–BCP-041 tasks, dependencies and acceptance evidence live only in the canonical detailed plan and CSV register.

## Work-package map

| Work package | Scope |
|---|---|
| BCP-0 | Discovery and architecture |
| BCP-1 | Identity, permissions and audit |
| BCP-2 | Public website and CMS |
| BCP-3 | Lead and CRM |
| BCP-4 | Ephemeral demo platform |
| BCP-5 | Registration and onboarding |
| BCP-6 | Super Admin Console |
| BCP-7 | Subscription, billing and licensing |
| BCP-8 | Customer Portal |
| BCP-9 | Support Center |
| BCP-10 | Notifications and monitoring |
| BCP-11 | QA, security and production readiness |

## Delivery principles

Keep BCP inside the ASP.NET Core Modular Monolith and Cloud Control Plane. Keep customer commercial metadata separate from campus operational records. Enforce distinct public/demo/portal/admin authentication boundaries.

## Status and change control

Task-level status is maintained only in the BCP CSV register. Update the detailed plan and register in the same PR. This summary contains no independent task status.
