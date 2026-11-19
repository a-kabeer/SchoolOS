# 56 — SchoolOS Glossary

- **Document ID:** GOV-GLOSSARY-001
- **Status:** Current reference; terminology baseline
- **Decision maturity:** Not applicable — glossary
- **Owner:** Product owner and technical lead
- **Last reviewed:** 2026-11-19
- **Source baseline:** `a-kabeer/SchoolOS` `main` @ `1f84bb022816a91d985c7c0baae77b3a5ca47121`; terminology reconciled with documents 00–53 and the v2.1 remediation draft

This glossary defines recurring SchoolOS terms. It does not approve product policy or replace the specifications and ADRs.

| Term | Meaning in SchoolOS |
|---|---|
| Tenant | The customer/account boundary used for platform-level tenancy and authorization. Exact mapping to legal organization/school entities remains governed by the approved data and platform specifications. |
| Organization | The customer-level business relationship represented in the Cloud Control Plane. Subscription and customer-platform records are organization-scoped as defined by the commercial and BCP specifications. |
| School | A school/legal or operational school entity within the tenant hierarchy. Do not assume it is always identical to an organization. |
| Campus | A physical/operational site. Campus scope is relevant to user access, academic operations and Local Node topology. |
| Cloud Control Plane | The cloud-hosted platform services for tenant/customer registry, subscriptions, billing, licensing, support and approved cloud-side metadata. |
| Local Data Plane | The school-operational application and database boundary hosted on a Local Node or an approved cloud-centralized deployment profile. |
| Local Node | The ASP.NET Core application host and PostgreSQL operational data store used by a campus or group of campuses. It is authoritative for accepted local operations within its configured scope. |
| Local Edge Node | A Local Node deployed at a campus that must continue operating independently when disconnected from head office or a shared group node. |
| Cloud Sync Hub | The cloud service that receives and distributes authorized events/projections between Local Nodes and cloud-side consumers. |
| PWA | Progressive Web App. In SchoolOS it supports the browser experience, cached assets and limited device-side drafts; it is not the authoritative offline backend. |
| Outbox / Inbox | Durable integration patterns used to record outgoing events atomically with local business writes and to deduplicate/track incoming events. |
| Idempotency | A property that makes retryng the same logical operation produce one business effect rather than duplicate records or postings. |
| Restricted Mode | License-expiry mode after the configured grace period. Historical viewing, approved reports/exports, backup, billing/licence renewal and support remain available while configured new operational writes are blocked. |
| Guardian | A person linked to a student through an explicitly recorded relationship. Guardian status alone does not automatically grant every data-access or communication right. |
| Household | A family/grouping abstraction used to relate students and people where approved; membership and data-access rules must not be inferred from surname or phone number alone. |
| Business & Customer Platform (BCP) | Cloud Control Plane experiences and services for prospective customers, registered organizations, customer portal, administration and support. |
| ADR | Architecture Decision Record. A controlled record of context, decision maturity, consequences and alternatives. |
| Decision maturity | Whether a direction is confirmed, accepted, proposed, open or superseded. It is distinct from implementation status. |
| Implementation status | Whether code, tests and operational evidence show work as not started, in progress, blocked, in review or verified complete. |

## Authority

When a glossary definition appears to conflict with a requirement or decision, follow the current approved ADR and the canonical product/architecture specification. Record material terminology changes in the relevant document and update this glossary in the same change.
