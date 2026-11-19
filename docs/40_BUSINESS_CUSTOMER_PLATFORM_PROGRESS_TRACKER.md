# 40 — Business & Customer Platform Progress Tracker

- **Canonical task plan:** [BCP Implementation Plan](task-plans/BCP_IMPLEMENTATION_TASK_PLAN.md)
- **Canonical task register:** [BCP task register](../registers/task-register-business-customer-platform.csv)
- **Requirements:** [29 — BCP Product Requirements](29_BUSINESS_CUSTOMER_PLATFORM_REQUIREMENTS.md)
- **Decision:** [ADR-020](decisions/ADR-020-BUSINESS-CUSTOMER-PLATFORM.md)

## Overall status

**Planning / Not Started.** Individual BCP task statuses are maintained only in the canonical CSV task register.

## Confirmed product direction

- Instant synthetic demo and personalized sales demo request.
- Session-bound ephemeral demo workspaces, with server-authoritative revocation and cleanup.
- Self-registration and manual company review/approval.
- Built-in ticketing/live chat/help center and WhatsApp integration.
- Public product/pricing website and internal Super Admin Console.
- BCP in the Cloud Control Plane, with separate identity audiences and no unrestricted access to campus operational data.

## Work-package map

| Work package | Scope |
|---|---|
| BCP-0 | Discovery and architecture |
| BCP-1 | Identity, permissions and audit |
| BCP-2 | Public website and CMS |
| BCP-3 | Lead and CRM |
| BCP-4 | Demo platform |
| BCP-5 | Registration and onboarding |
| BCP-6 | Super Admin Console |
| BCP-7 | Subscription, billing and licensing |
| BCP-8 | Customer Portal |
| BCP-9 | Support Center |
| BCP-10 | Notifications and monitoring |
| BCP-11 | QA, security and production readiness |

## Open dependencies

- Identity provider, session strategy and MFA policy.
- Customer/staff permission matrix and separation of duties.
- Demo storage isolation, concurrency limits and timeout values.
- WhatsApp provider, consent and template policy.
- Support staffing, hours and service levels.
- Data retention, deletion, export and legal review.
- Approved commercial pricing and payment provider.
- Production node and customer-support access policy.

## Status governance

Do not copy task-level statuses into this document. Update the canonical CSV register and detailed plan in the same PR. A discussion or design note alone does not mark implementation complete.
