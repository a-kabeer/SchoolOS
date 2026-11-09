# ADR-020 — Business & Customer Platform Ecosystem

- **Date:** 9 November 2026
- **Status:** Accepted direction for planning; implementation and detailed designs pending
- **Decision owners:** Product owner + technical lead

## Context

SchoolOS requires a complete commercial and customer lifecycle experience beyond the campus school-management application: public product discovery, demos, registration, customer account management, subscription/licence visibility, company administration and support. These functions must operate without weakening the separation between Cloud Control Plane and campus Local Data Planes.

## Decision

Create a **SchoolOS Business & Customer Platform (BCP)** within the Cloud Control Plane. It will include five audience-specific frontends/experiences:

1. Public Marketing Website + CMS.
2. Demo Platform.
3. Customer Portal.
4. Internal Super Admin Console.
5. Support Center.

The backend remains part of the ASP.NET Core Modular Monolith with React + TypeScript frontends and PostgreSQL/EF Core. Frontends may be separate deployments/hostnames and must use separate authentication/session boundaries according to audience and privilege. Cloud Control Plane stores commercial/customer/node metadata and approved projections; it is not a blanket gateway into campus operational data.

The following product choices are confirmed:
- Demo supports both instant self-service and personalized sales demo request.
- Registration supports both self-service and manual company review/approval.
- Support includes built-in ticketing/live chat/help center and WhatsApp integration.
- Public website presents full product information and pricing, with Try Demo and registration/contact paths.
- Company staff use a Super Admin Console to manage organizations and platform service issues.

## Security and data constraints

- Demo datasets are synthetic, resettable, time-limited and isolated from production.
- Self-registration does not automatically grant production approval, subscription or licence.
- Organization approval, payment verification, subscription activation, node registration and licence issuance are separate controlled steps.
- Customer portal and staff roles are not the same as campus application roles.
- Admin access to student/fee/exam records is not granted by default.
- Remote support access requires explicit customer approval, scope, expiry and audit; default read-only.
- WhatsApp is not used to exchange passwords, private keys or sensitive school records.
- Public pricing content exposes only approved published tariff data.

## Consequences

Positive:
- Creates a clear commercial lifecycle from discovery to onboarding, renewal and support.
- Enables customers to self-serve commercial tasks while preserving local operations.
- Gives company teams operational visibility with clear boundaries and auditability.
- Supports separate frontend experiences for distinct audiences.

Costs/risks:
- Adds significant scope across identity, CRM, demo, billing, licensing, support and CMS.
- Requires strong tenant isolation, session boundaries, privileged staff controls and retention policy before launch.
- Requires ongoing third-party provider and support operations for messaging/live chat/WhatsApp.

## Alternatives considered

- Put all features inside the campus SchoolOS application: rejected because public prospects, customer finance contacts, SchoolOS staff and campus users have different trust boundaries and permissions.
- Use separate microservices for each frontend: not selected initially; retain Modular Monolith unless scale/team boundaries justify extraction.
- Allow self-registration to activate production automatically: rejected due to organization verification, billing, node readiness and licence risk.

## Follow-up

Implement discovery and role/data-flow design in BCP-0/BCP-1. Track all implementation work in `docs/39_BUSINESS_CUSTOMER_PLATFORM_TASK_PLAN.md` and `docs/40_BUSINESS_CUSTOMER_PLATFORM_PROGRESS_TRACKER.md`.
